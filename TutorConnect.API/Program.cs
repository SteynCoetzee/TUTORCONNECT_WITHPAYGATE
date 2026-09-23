using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using TutorConnect.API.Data;
using TutorConnect.API.Models;
using TutorConnect.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Raise Kestrel's default 30 MB body limit to support large video uploads (up to 600 MB)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 600_000_000; // 600 MB
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS: allow the Angular dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5149", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add services to the container.
builder.Services.AddControllers(options =>
    {
        // Remove text/plain formatter so string returns are always JSON-encoded.
        // Without this, Ok("message") returns text/plain which Angular's JSON
        // HttpClient can't parse, triggering false error callbacks.
        options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.StringOutputFormatter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Most DTO fields have a custom, human-written [Required]/[StringLength]/etc. ErrorMessage
// (e.g. "Assignment name is required."). Wherever one doesn't, ASP.NET Core's built-in
// [ApiController] model validation falls back to a generic message built from the raw C#
// property name, e.g. "The Module_Code field is required." - readable to a developer, not
// to a user. This rewrites just the underscores in those messages ("Module_Code" ->
// "Module Code") before the response is built, so every validation error across the whole
// API reads like an English sentence without having to hand-annotate every single attribute.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    var defaultFactory = options.InvalidModelStateResponseFactory;
    options.InvalidModelStateResponseFactory = context =>
    {
        foreach (var entry in context.ModelState.Values)
        {
            if (entry.Errors.Count == 0) continue;
            var rewritten = entry.Errors
                .Select(e => e.Exception != null
                    ? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelError(e.Exception, e.ErrorMessage.Replace('_', ' '))
                    : new Microsoft.AspNetCore.Mvc.ModelBinding.ModelError(e.ErrorMessage.Replace('_', ' ')))
                .ToList();
            entry.Errors.Clear();
            foreach (var e in rewritten) entry.Errors.Add(e);
        }
        return defaultFactory(context);
    };
});
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<GoogleCalendarService>();
builder.Services.AddSingleton<CloudinaryService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Global exception handler - catches any unhandled exception and returns a
// clean JSON 500 instead of leaking stack traces or raw exception messages.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (feature?.Error != null)
        {
            var log = context.RequestServices
                .GetRequiredService<ILogger<Program>>();
            log.LogError(feature.Error, "Unhandled exception");
        }
        context.Response.StatusCode  = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            "{\"error\":\"An unexpected error occurred. Please try again later.\"}");
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ── Auto-migrate + seed roles/hardcoded admin ─────────────────────────────────
// Applies any pending EF Core migrations on startup, so a fresh clone just needs
// a reachable SQL Server (per appsettings.json's connection string) - no manual
// `dotnet ef database update` step. Safe to run every time: Migrate() is a no-op
// once the database is already up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Seed User_Roles if the table is empty (fresh database)
    if (!db.User_Roles.Any())
    {
        db.User_Roles.AddRange(
            new User_Role { User_Role_Name = "Admin" },
            new User_Role { User_Role_Name = "Tutor" },
            new User_Role { User_Role_Name = "Student" },
            new User_Role { User_Role_Name = "AW-Tutor" }
        );
        db.SaveChanges();
    }

    // Seed hardcoded admin user
    const string adminEmail = "TutorConnect00@gmail.com";
    if (!db.Users.Any(u => u.Email == adminEmail))
    {
        var adminRoleId = db.User_Roles.First(r => r.User_Role_Name == "Admin").User_Role_ID;
        db.Users.Add(new User
        {
            FirstName    = "ADMIN",
            LastName     = "ADMIN",
            Email        = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TutorConnect123!"),
            User_Role_ID = adminRoleId,
            Phone        = "ADMIN",
            Address      = "ADMIN",
            Bio          = "System administrator account."
        });
        db.SaveChanges();
    }
}
// ─────────────────────────────────────────────────────────────────────────────

// ── Startup: get the live PayFast webhook working with zero manual steps ──────
// PayFast:NotifyUrl points at a reserved ngrok static domain (see Start-Ngrok.bat) -
// PayFast's own servers call it after every completed payment to confirm it back
// here. If that tunnel isn't running, a real payment still succeeds on PayFast's
// side (the customer is charged) but the confirmation never reaches this backend -
// silently, with nothing in the app itself to show it. Instead of relying on
// someone remembering to run Start-Ngrok.bat first, the backend now does the
// whole thing itself on startup: find ngrok (installing it via winget if it's
// missing entirely) -> launch the tunnel if it isn't already up -> verify the
// result is actually reachable. Runs in the background after the server has
// started listening, so none of this ever delays startup.
//
// This can only prove the URL is reachable, not that PayFast itself can reach it
// (e.g. it won't catch a firewall that specifically blocks PayFast's IP ranges),
// and the static domain only ever works when run from the machine whose ngrok
// account owns it - anyone else needs their own ngrok account + domain (and
// PayFast:NotifyUrl pointed at it) to receive real callbacks. Set
// PayFast:AutoStartNgrok to false in appsettings.json to disable all of this
// (e.g. once real production hosting replaces ngrok entirely).
app.Lifetime.ApplicationStarted.Register(() => _ = EnsurePayFastTunnelAsync(app));

app.Run();

// Finds ngrok, launches the tunnel if it isn't already running, then always runs
// the reachability check so the result is reported either way.
static async Task EnsurePayFastTunnelAsync(WebApplication app)
{
    var config  = app.Configuration;
    var payFast = config.GetSection("PayFast");

    if (!payFast.GetValue("AutoStartNgrok", true))
    {
        await CheckPayFastNotifyUrlAsync(config);
        return;
    }

    var notifyUrl = payFast["NotifyUrl"];
    if (string.IsNullOrWhiteSpace(notifyUrl) || !Uri.TryCreate(notifyUrl, UriKind.Absolute, out var notifyUri))
    {
        await CheckPayFastNotifyUrlAsync(config); // reports the missing/invalid URL itself
        return;
    }

    var domain = notifyUri.Host;
    var port   = GetListeningHttpPort(app);

    if (await IsNgrokTunnelUpAsync(domain))
    {
        Console.WriteLine($"[TutorConnect] ngrok tunnel for {domain} is already running.");
    }
    else
    {
        var ngrokExe = FindNgrokExe() ?? await InstallNgrokAsync();
        if (ngrokExe == null)
        {
            Console.WriteLine("[TutorConnect] Could not find or install ngrok - install it manually from https://ngrok.com/download, or run Start-Ngrok.bat once you have.");
        }
        else
        {
            await LaunchNgrokTunnelAsync(app, ngrokExe, domain, port);
        }
    }

    await CheckPayFastNotifyUrlAsync(config);
}

// Looks for ngrok.exe on PATH first, then the WinGet packages folder (mirrors
// Start-Ngrok.bat's own lookup, so both agree on where it'd expect to find it).
static string? FindNgrokExe()
{
    var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
    foreach (var dir in pathEnv.Split(Path.PathSeparator))
    {
        try
        {
            var candidate = Path.Combine(dir, "ngrok.exe");
            if (File.Exists(candidate)) return candidate;
        }
        catch { /* malformed PATH entry - skip it */ }
    }

    try
    {
        var wingetRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WinGet", "Packages");
        if (Directory.Exists(wingetRoot))
            return Directory.EnumerateFiles(wingetRoot, "ngrok.exe", SearchOption.AllDirectories).FirstOrDefault();
    }
    catch { /* no access to the folder, or it doesn't exist - fine, just not found */ }

    return null;
}

// ngrok isn't on this PC at all - install it with winget (built into Windows 10/11,
// so this needs nothing extra). Non-interactive: --silent plus both agreement
// flags so it never blocks on a prompt the backend can't answer.
static async Task<string?> InstallNgrokAsync()
{
    Console.WriteLine("[TutorConnect] ngrok.exe not found on this PC - installing it via winget...");
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "winget",
            Arguments              = "install --id Ngrok.Ngrok -e --accept-package-agreements --accept-source-agreements --silent",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        };
        using var proc = Process.Start(psi);
        if (proc == null)
        {
            Console.WriteLine("[TutorConnect] Could not start winget.");
            return null;
        }

        // Downloads can take a while on a slow connection - worth a real wait, but
        // capped so a stuck installer can't hang the check forever.
        var exited = await Task.Run(() => proc.WaitForExit(120_000));
        if (!exited)
        {
            Console.WriteLine("[TutorConnect] winget install is taking too long - giving up for this run.");
            try { proc.Kill(true); } catch { }
            return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TutorConnect] Could not run winget to install ngrok: {ex.Message}");
        Console.WriteLine("[TutorConnect]   Install it manually from https://ngrok.com/download, or run Start-Ngrok.bat once you have.");
        return null;
    }

    var exe = FindNgrokExe();
    if (exe == null)
        Console.WriteLine("[TutorConnect] ngrok still isn't found after installing - a new terminal session may be needed for PATH to pick it up. Install it manually from https://ngrok.com/download if this keeps happening.");
    else
        Console.WriteLine($"[TutorConnect] ngrok installed: {exe}");
    return exe;
}

// Launches the tunnel and waits (briefly) for it to actually come up before
// returning, so the reachability check right after this has a fair chance to pass.
static async Task LaunchNgrokTunnelAsync(WebApplication app, string ngrokExe, string domain, int port)
{
    Console.WriteLine($"[TutorConnect] Launching ngrok tunnel: {domain} -> localhost:{port} ...");
    var errorLines = new List<string>();
    Process? proc;
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName               = ngrokExe,
            Arguments              = $"http --domain={domain} {port} --log=stdout",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        };
        proc = Process.Start(psi);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TutorConnect] Failed to launch ngrok: {ex.Message}");
        return;
    }
    if (proc == null) { Console.WriteLine("[TutorConnect] Failed to launch ngrok."); return; }

    // Never leave an orphaned tunnel bound to the static domain after this backend
    // stops - that would block the next run (or Start-Ngrok.bat) from using it.
    app.Lifetime.ApplicationStopping.Register(() =>
    {
        try { if (!proc.HasExited) proc.Kill(true); } catch { /* already gone */ }
    });

    proc.ErrorDataReceived += (_, e) => { if (e.Data != null) errorLines.Add(e.Data); };
    proc.BeginErrorReadLine();

    var up = false;
    for (var i = 0; i < 10 && !up; i++)
    {
        await Task.Delay(1000);
        up = await IsNgrokTunnelUpAsync(domain);
    }

    if (up)
    {
        Console.WriteLine("[TutorConnect] ngrok tunnel is up.");
    }
    else
    {
        Console.WriteLine("[TutorConnect] ngrok did not come up in time.");
        if (errorLines.Count > 0)
            Console.WriteLine("[TutorConnect]   ngrok said: " + string.Join(" | ", errorLines.TakeLast(3)));
        Console.WriteLine("[TutorConnect]   This static domain only works from the machine whose ngrok account owns it. If this isn't");
        Console.WriteLine("[TutorConnect]   that machine, run 'ngrok config add-authtoken <your token>' with your own account, then point");
        Console.WriteLine("[TutorConnect]   PayFast:NotifyUrl at your own domain instead.");
    }
}

// Asks ngrok's own local agent API (always at 127.0.0.1:4040 while a tunnel is
// running) whether it currently has a tunnel open for the given domain. This is
// how both "is one already running" and "did the one we just launched come up"
// are checked - no PayFast/network round-trip needed for either.
static async Task<bool> IsNgrokTunnelUpAsync(string domain)
{
    try
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var json = await http.GetStringAsync("http://127.0.0.1:4040/api/tunnels");
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        foreach (var tunnel in doc.RootElement.GetProperty("tunnels").EnumerateArray())
        {
            var publicUrl = tunnel.GetProperty("public_url").GetString() ?? "";
            if (publicUrl.Contains(domain, StringComparison.OrdinalIgnoreCase))
                return true;
        }
    }
    catch { /* ngrok's local API isn't up (yet, or at all) - just means "not running" */ }
    return false;
}

// The port this instance is actually listening on (not hardcoded), so the tunnel
// always forwards to wherever the backend really is, even if ASPNETCORE_URLS
// overrides the documented default dev port.
static int GetListeningHttpPort(WebApplication app)
{
    var addresses = app.Services
        .GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
        .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()
        ?.Addresses ?? Enumerable.Empty<string>();

    foreach (var addr in addresses)
        if (Uri.TryCreate(addr, UriKind.Absolute, out var uri) && uri.Scheme == "http")
            return uri.Port;

    return 5149; // falls back to the documented default dev port if nothing matched
}

// Fires a HEAD request at the configured PayFast notify URL. HEAD (not POST) is
// deliberate: /api/PayFast/notify is a [HttpPost]-only route, so HEAD can never
// trigger real payment-confirmation logic - it only proves the request reached
// the server at all. Any HTTP response (even a 404/405) means "reachable"; a
// thrown exception (DNS failure, connection refused, timeout) means it isn't.
static async Task CheckPayFastNotifyUrlAsync(IConfiguration configuration)
{
    var payFast    = configuration.GetSection("PayFast");
    var notifyUrl  = payFast["NotifyUrl"];
    var merchantId = payFast["MerchantId"];
    var isSandbox  = payFast.GetValue<bool>("IsSandbox");

    Console.WriteLine();
    Console.WriteLine("[TutorConnect] -- PayFast startup check -----------------------------------");

    if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(payFast["MerchantKey"]))
    {
        Console.WriteLine("[TutorConnect] X PayFast MerchantId/MerchantKey missing from configuration - payments will not work at all.");
    }
    else if (string.IsNullOrWhiteSpace(notifyUrl) || !Uri.TryCreate(notifyUrl, UriKind.Absolute, out _))
    {
        Console.WriteLine("[TutorConnect] X PayFast:NotifyUrl is missing or not a valid URL - PayFast has no way to confirm payments back to this server.");
    }
    else
    {
        Console.WriteLine($"[TutorConnect] Mode: {(isSandbox ? "SANDBOX" : "LIVE")}  |  Notify URL: {notifyUrl}");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            var request = new HttpRequestMessage(HttpMethod.Head, notifyUrl);
            request.Headers.Add("ngrok-skip-browser-warning", "true"); // bypass ngrok's free-tier interstitial page
            var response = await http.SendAsync(request);

            if (response.Headers.TryGetValues("Ngrok-Error-Code", out var ngrokErrors))
            {
                Console.WriteLine($"[TutorConnect] X PayFast notify URL is NOT reachable - ngrok tunnel is not running ({string.Join(", ", ngrokErrors)}).");
                Console.WriteLine("[TutorConnect]   Start the ngrok tunnel (see Start-Ngrok.bat) and confirm its address still matches PayFast:NotifyUrl in appsettings.json.");
            }
            else
            {
                Console.WriteLine($"[TutorConnect] OK PayFast notify URL is reachable (HTTP {(int)response.StatusCode}). Live payments will be recorded correctly.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TutorConnect] X PayFast notify URL is NOT reachable: {ex.Message}");
            Console.WriteLine("[TutorConnect]   Payments will still succeed on PayFast's side but won't be recorded here until this is fixed.");
            Console.WriteLine("[TutorConnect]   Check that your ngrok tunnel is running and that PayFast:NotifyUrl in appsettings.json matches its current address.");
        }
    }

    Console.WriteLine("[TutorConnect] ---------------------------------------------------------------");
    Console.WriteLine();
}
