using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
// property name, e.g. "The Module_Code field is required." — readable to a developer, not
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

// Global exception handler — catches any unhandled exception and returns a
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
// a reachable SQL Server (per appsettings.json's connection string) — no manual
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

// ── Startup self-check: is the live PayFast webhook actually reachable? ───────
// PayFast:NotifyUrl (appsettings.json) is a fixed, hand-maintained URL — currently
// an ngrok tunnel — that PayFast's own servers call after every completed payment
// to confirm it. If that tunnel isn't running, or its address has drifted since
// appsettings.json was last updated, a real payment still succeeds on PayFast's
// side (the customer is charged) but the confirmation never reaches this backend —
// silently, with nothing in the app itself to show it. Checking this on every
// startup turns that into an impossible-to-miss console message instead of a
// support ticket after the fact.
//
// This can only prove the URL is reachable, not that PayFast itself can reach it
// (e.g. it won't catch a firewall that specifically blocks PayFast's IP ranges),
// and a free ngrok tunnel's address changes every time ngrok restarts unless a
// reserved/static domain is used — see the pending ngrok task. Runs after the
// server has started listening, in the background, so it never delays startup.
app.Lifetime.ApplicationStarted.Register(() => _ = CheckPayFastNotifyUrlAsync(app.Configuration));

app.Run();

// Fires a HEAD request at the configured PayFast notify URL. HEAD (not POST) is
// deliberate: /api/PayFast/notify is a [HttpPost]-only route, so HEAD can never
// trigger real payment-confirmation logic — it only proves the request reached
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
        Console.WriteLine("[TutorConnect] X PayFast MerchantId/MerchantKey missing from configuration — payments will not work at all.");
    }
    else if (string.IsNullOrWhiteSpace(notifyUrl) || !Uri.TryCreate(notifyUrl, UriKind.Absolute, out _))
    {
        Console.WriteLine("[TutorConnect] X PayFast:NotifyUrl is missing or not a valid URL — PayFast has no way to confirm payments back to this server.");
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
                Console.WriteLine($"[TutorConnect] X PayFast notify URL is NOT reachable — ngrok tunnel is not running ({string.Join(", ", ngrokErrors)}).");
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
