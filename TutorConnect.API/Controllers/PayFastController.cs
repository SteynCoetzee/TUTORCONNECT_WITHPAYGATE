using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TutorConnect.API.Data;
using TutorConnect.API.Models;

namespace TutorConnect.API.Controllers
{
    // ── DTOs ────────────────────────────────────────────────────────────────────

    public class PayFastInitiateDto
    {
        public int StudentId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PayFastLineItem> Items { get; set; } = new();
    }

    public class PayFastLineItem
    {
        public string ModuleCode { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string SessionType { get; set; } = string.Empty; // "OneOnOne" or "Group"
        public decimal Price { get; set; }
    }

    // ── Controller ──────────────────────────────────────────────────────────────
    //
    // The automated card/EFT gateway flow (separate from the manual bank-reference
    // flow in PaymentsController). Money only ever actually changes the DB to
    // "Paid" in one place — Notify(), the server-to-server callback PayFast makes
    // once a payment truly clears. Everything else here either starts that flow,
    // reports on it, or exists to cosmetically bounce the browser around it:
    //   1. STUDENT-FACING FLOW    Initiate — student starts a payment
    //   2. PAYFAST CALLBACKS      RedirectSuccess/RedirectCancel are cosmetic
    //                             browser bounces only; Notify is the real,
    //                             signature-verified server-to-server ITN that
    //                             actually marks a payment Paid and enrolls the student
    //   3. ADMIN                  GetAllPayments (read) / AdminProcess (manual
    //                             fallback for when PayFast's ITN never arrived)
    //   4. SIGNATURE HELPERS      PayFast's MD5 signature scheme, private
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [ApiController]
    public class PayFastController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;
        private readonly ILogger<PayFastController> _logger;

        public PayFastController(IConfiguration config, AppDbContext db, ILogger<PayFastController> logger)
        {
            _config = config;
            _db = db;
            _logger = logger;
        }

        // ── 1. STUDENT-FACING FLOW ──────────────────────────────────────────────

        // ── POST /api/PayFast/initiate ──────────────────────────────────────────
        // Called by Angular before redirecting to PayFast.
        // Creates a Pending Payment record and returns the signed form fields.
        [HttpPost("initiate")]
        [Authorize]
        public async Task<ActionResult> Initiate([FromBody] PayFastInitiateDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest("No items provided.");

            var student = await _db.Users.FindAsync(dto.StudentId);
            if (student == null) return NotFound("Student not found.");

            // Recompute total from DB — never trust the client-supplied amount
            decimal computedTotal = 0m;
            foreach (var item in dto.Items)
            {
                var module = await _db.Modules.FindAsync(item.ModuleCode);
                if (module == null) return BadRequest($"Module '{item.ModuleCode}' not found.");
                computedTotal += item.SessionType == "OneOnOne"
                    ? module.Module_Price_OneOnOne
                    : module.Module_Price_Group;
            }
            if (computedTotal <= 0)
                return BadRequest("Payment amount is R0.00. Please ask an administrator to set module prices before enrolling.");

            // Idempotency guard: block duplicate Pending payments for the same student + amount.
            // computedTotal is derived from DB prices so equality is exact.
            var hasPending = await _db.Payments.AnyAsync(p =>
                p.Student_ID == dto.StudentId &&
                p.Payment_Status == "Pending" &&
                p.Amount == computedTotal);
            if (hasPending)
                return BadRequest("A pending payment of the same amount already exists for your account. Please wait for it to be processed, or contact support to resolve it.");

            var cfg = _config.GetSection("PayFast");
            var merchantId  = cfg["MerchantId"]!;
            var merchantKey = cfg["MerchantKey"]!;
            var passphrase  = cfg["Passphrase"]!;
            var isSandbox   = bool.Parse(cfg["IsSandbox"] ?? "false");
            var returnUrl   = cfg["ReturnUrl"]!;
            var cancelUrl   = cfg["CancelUrl"]!;
            var notifyUrl   = cfg["NotifyUrl"]!;

            // Create a Pending payment record
            var payment = new Payment
            {
                Amount             = computedTotal,
                Payment_Date       = DateTime.UtcNow,
                Payment_Status     = "Pending",
                Student_ID         = dto.StudentId,
                Module_Code        = dto.Items[0].ModuleCode,          // primary FK
                Enrollment_Items_Json = JsonSerializer.Serialize(dto.Items) // full cart
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            var itemDesc = string.Join(", ", dto.Items.Select(i => $"{i.ModuleName} ({i.SessionType})"));
            if (itemDesc.Length > 255) itemDesc = itemDesc[..252] + "...";

            // Build PayFast form fields — ORDER MATTERS for the signature
            var fields = new Dictionary<string, string>
            {
                ["merchant_id"]   = merchantId,
                ["merchant_key"]  = merchantKey,
                ["return_url"]    = returnUrl,
                ["cancel_url"]    = cancelUrl,
                ["notify_url"]    = notifyUrl,
                ["name_first"]    = student.FirstName ?? "Student",
                ["name_last"]     = student.LastName  ?? "User",
                ["email_address"] = student.Email,
                ["m_payment_id"]  = payment.Payment_ID.ToString(),
                ["amount"]        = computedTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                ["item_name"]     = "TutorConnect Module Enrollment",
                ["item_description"] = itemDesc,
                ["custom_int1"]   = dto.StudentId.ToString()
            };

            fields["signature"] = GenerateSignature(fields, passphrase);

            var payFastUrl = isSandbox
                ? "https://sandbox.payfast.co.za/eng/process"
                : "https://www.payfast.co.za/eng/process";

            return Ok(new { payFastUrl, formData = fields });
        }

        // ── 2. PAYFAST CALLBACKS ─────────────────────────────────────────────────

        // ── GET /api/PayFast/redirect-success & redirect-cancel ─────────────────
        // PayFast cannot redirect to localhost — so it redirects to these public
        // tunnel endpoints, which then bounce the browser to the Angular frontend.
        // Cosmetic only — neither of these ever touches the database; a payment is
        // only ever marked Paid by Notify() below, once PayFast's own server confirms it.
        [HttpGet("redirect-success")]
        [AllowAnonymous]
        public IActionResult RedirectSuccess()
        {
            var frontendUrl = _config["PayFast:FrontendUrl"] ?? "http://localhost:4200";
            return Redirect($"{frontendUrl}/dashboard/payment-result?status=success");
        }

        [HttpGet("redirect-cancel")]
        [AllowAnonymous]
        public IActionResult RedirectCancel()
        {
            var frontendUrl = _config["PayFast:FrontendUrl"] ?? "http://localhost:4200";
            return Redirect($"{frontendUrl}/dashboard/payment-result?status=cancel");
        }

        // ── POST /api/PayFast/notify ────────────────────────────────────────────
        // PayFast's Instant Transaction Notification — server-to-server.
        // Requires a PUBLICLY ACCESSIBLE URL (won't fire on localhost).
        // When deployed, PayFast will POST here after every completed/failed payment.
        [HttpPost("notify")]
        [AllowAnonymous]
        public async Task<ActionResult> Notify([FromForm] IFormCollection form)
        {
            _logger.LogInformation("[PayFast ITN] Received notify POST with {Count} fields.", form.Count);

            var pfData = form.ToDictionary(k => k.Key, v => v.Value.ToString());

            // Log every field PayFast sent (helps diagnose signature issues)
            foreach (var kv in pfData)
                _logger.LogInformation("[PayFast ITN] Field: {Key} = [{Value}]", kv.Key, kv.Value);

            // 1. Verify signature — include ALL fields (even empty ones), matching PHP urlencode behaviour
            var passphrase  = _config["PayFast:Passphrase"]!;
            var receivedSig = pfData.GetValueOrDefault("signature", "");
            var dataForSig  = pfData.Where(k => k.Key != "signature")
                                    .ToDictionary(k => k.Key, v => v.Value);
            var paramString = BuildParamString(dataForSig, passphrase, includeEmpty: true);
            var computedSig = GenerateSignatureFromString(paramString);

            _logger.LogInformation("[PayFast ITN] Param string: {ParamString}", paramString);
            _logger.LogInformation("[PayFast ITN] Received signature : {Received}", receivedSig);
            _logger.LogInformation("[PayFast ITN] Computed signature : {Computed}", computedSig);

            if (computedSig != receivedSig)
            {
                _logger.LogWarning("[PayFast ITN] SIGNATURE MISMATCH — ITN rejected.");
                return BadRequest("Invalid signature");
            }

            // 2. Handle payment status
            var paymentStatus = pfData.GetValueOrDefault("payment_status");
            _logger.LogInformation("[PayFast ITN] payment_status={Status}", paymentStatus);

            // If payment failed (e.g. insufficient funds, card declined) — mark it Failed in DB
            if (paymentStatus == "FAILED")
            {
                if (int.TryParse(pfData.GetValueOrDefault("m_payment_id"), out var failedPaymentId))
                {
                    var failedPayment = await _db.Payments.FindAsync(failedPaymentId);
                    if (failedPayment != null && failedPayment.Payment_Status == "Pending")
                    {
                        failedPayment.Payment_Status = "Failed";
                        await _db.SaveChangesAsync();
                        _logger.LogWarning("[PayFast ITN] Payment {PaymentId} marked as Failed.", failedPaymentId);
                    }
                }
                return Ok();
            }

            // Only enroll for COMPLETE payments — ignore PENDING or any other status
            if (paymentStatus != "COMPLETE")
            {
                _logger.LogInformation("[PayFast ITN] Ignoring non-actionable status: {Status}", paymentStatus);
                return Ok();
            }

            // 3. Look up the payment record
            if (!int.TryParse(pfData.GetValueOrDefault("m_payment_id"), out var paymentId))
            {
                _logger.LogWarning("[PayFast ITN] Missing or invalid m_payment_id.");
                return BadRequest("Missing payment ID");
            }
            _logger.LogInformation("[PayFast ITN] m_payment_id={PaymentId}", paymentId);

            var payment = await _db.Payments.FindAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning("[PayFast ITN] Payment {PaymentId} not found in DB.", paymentId);
                return NotFound();
            }

            // 4. Verify amount — always parse with InvariantCulture; PayFast sends "3400.00"
            var amountStr = pfData.GetValueOrDefault("amount_gross", "");
            _logger.LogInformation("[PayFast ITN] amount_gross={Amount} | DB amount={DbAmount}", amountStr, payment.Amount);
            if (!decimal.TryParse(amountStr,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var gross))
            {
                _logger.LogWarning("[PayFast ITN] Could not parse amount_gross.");
                return BadRequest("Missing amount");
            }
            if (Math.Round(gross, 2) != Math.Round(payment.Amount, 2))
            {
                _logger.LogWarning("[PayFast ITN] AMOUNT MISMATCH: got {Got}, expected {Expected}.", gross, payment.Amount);
                return BadRequest("Amount mismatch");
            }

            // 5. Mark payment as Paid
            payment.Payment_Status    = "Paid";
            payment.Payment_Reference = pfData.GetValueOrDefault("pf_payment_id", "");

            // 6. Process enrollments from the stored JSON
            // Group by module code first so that OneOnOne + Group for the same module
            // always produce a single Student_Modules row, not two.
            var studentId = int.Parse(pfData.GetValueOrDefault("custom_int1", "0"));
            if (studentId > 0 && !string.IsNullOrEmpty(payment.Enrollment_Items_Json))
            {
                var items = JsonSerializer.Deserialize<List<PayFastLineItem>>(payment.Enrollment_Items_Json);
                if (items != null)
                {
                    foreach (var moduleGroup in items.GroupBy(i => i.ModuleCode))
                    {
                        var moduleCode  = moduleGroup.Key;
                        var wantOneOnOne = moduleGroup.Any(i => i.SessionType == "OneOnOne");
                        var wantGroup    = moduleGroup.Any(i => i.SessionType == "Group");

                        var existing = await _db.Student_Modules
                            .FirstOrDefaultAsync(sm =>
                                sm.Student_ID  == studentId &&
                                sm.Module_Code == moduleCode &&
                                sm.IsActive);

                        if (existing != null)
                        {
                            if (wantOneOnOne) { existing.Sessions_Remaining_OneOnOne += 5; existing.Can_Book_OneOnOne = true; }
                            if (wantGroup)    { existing.Sessions_Remaining_Group    += 5; existing.Can_Book_Group    = true; }
                        }
                        else
                        {
                            _db.Student_Modules.Add(new Student_Module
                            {
                                Student_ID                  = studentId,
                                Module_Code                 = moduleCode,
                                Enrollment_Date             = DateTime.UtcNow,
                                IsActive                    = true,
                                Can_Book_OneOnOne           = wantOneOnOne,
                                Can_Book_Group              = wantGroup,
                                Sessions_Remaining_OneOnOne = wantOneOnOne ? 5 : 0,
                                Sessions_Remaining_Group    = wantGroup    ? 5 : 0
                            });
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("[PayFast ITN] Payment {PaymentId} fully processed — enrollments saved.", paymentId);
            return Ok();
        }

        // ── 3. ADMIN ─────────────────────────────────────────────────────────────

        // ── GET /api/PayFast/payments ───────────────────────────────────────────
        // Admin view: list all payments with student info so stuck Pending ones can be reprocessed.
        [HttpGet("payments")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetAllPayments()
        {
            var payments = await _db.Payments
                .OrderByDescending(p => p.Payment_Date)
                .Select(p => new
                {
                    p.Payment_ID,
                    p.Amount,
                    p.Payment_Date,
                    p.Payment_Status,
                    p.Module_Code,
                    p.Payment_Reference,
                    p.Enrollment_Items_Json,
                    StudentName = _db.Users
                        .Where(u => u.User_ID == p.Student_ID)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault() ?? "Unknown",
                    StudentEmail = _db.Users
                        .Where(u => u.User_ID == p.Student_ID)
                        .Select(u => u.Email)
                        .FirstOrDefault() ?? "",
                    p.Student_ID
                })
                .ToListAsync();

            return Ok(payments);
        }

        // ── POST /api/PayFast/admin/process/{paymentId} ────────────────────────
        // Manual fallback: processes a Pending payment without waiting for ITN.
        // Use when PayFast ITN was not delivered (e.g. tunnel was down).
        [HttpPost("admin/process/{paymentId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AdminProcess(int paymentId)
        {
            var payment = await _db.Payments.FindAsync(paymentId);
            if (payment == null) return NotFound("Payment not found.");
            if (payment.Payment_Status == "Paid") return Ok("Already processed.");
            if (string.IsNullOrEmpty(payment.Enrollment_Items_Json))
                return BadRequest("No enrollment items stored for this payment.");

            var items = JsonSerializer.Deserialize<List<PayFastLineItem>>(payment.Enrollment_Items_Json);
            if (items == null || items.Count == 0) return BadRequest("Empty item list.");

            var studentId = payment.Student_ID;
            foreach (var moduleGroup in items.GroupBy(i => i.ModuleCode))
            {
                var moduleCode   = moduleGroup.Key;
                var wantOneOnOne = moduleGroup.Any(i => i.SessionType == "OneOnOne");
                var wantGroup    = moduleGroup.Any(i => i.SessionType == "Group");

                var existing = await _db.Student_Modules
                    .FirstOrDefaultAsync(sm =>
                        sm.Student_ID  == studentId &&
                        sm.Module_Code == moduleCode &&
                        sm.IsActive);

                if (existing != null)
                {
                    if (wantOneOnOne) { existing.Sessions_Remaining_OneOnOne += 5; existing.Can_Book_OneOnOne = true; }
                    if (wantGroup)    { existing.Sessions_Remaining_Group    += 5; existing.Can_Book_Group    = true; }
                }
                else
                {
                    _db.Student_Modules.Add(new Student_Module
                    {
                        Student_ID                  = studentId,
                        Module_Code                 = moduleCode,
                        Enrollment_Date             = DateTime.UtcNow,
                        IsActive                    = true,
                        Can_Book_OneOnOne           = wantOneOnOne,
                        Can_Book_Group              = wantGroup,
                        Sessions_Remaining_OneOnOne = wantOneOnOne ? 5 : 0,
                        Sessions_Remaining_Group    = wantGroup    ? 5 : 0
                    });
                }
            }

            payment.Payment_Status = "Paid";
            await _db.SaveChangesAsync();
            return Ok($"Enrolled {items.GroupBy(i => i.ModuleCode).Count()} module(s) for student {studentId}.");
        }

        // ── 4. SIGNATURE HELPERS (private — PayFast's MD5 signature scheme) ────

        // ── PayFast MD5 signature ───────────────────────────────────────────────
        private static string BuildParamString(Dictionary<string, string> data, string passphrase, bool includeEmpty = false)
        {
            var parts = data
                .Where(kv => includeEmpty || !string.IsNullOrEmpty(kv.Value?.Trim()))
                .Select(kv => $"{kv.Key}={PhpUrlEncode(kv.Value?.Trim() ?? "")}");

            var paramString = string.Join("&", parts);

            var pp = passphrase?.Trim() ?? "";
            if (!string.IsNullOrEmpty(pp))
                paramString += $"&passphrase={PhpUrlEncode(pp)}";

            return paramString;
        }

        private static string GenerateSignature(Dictionary<string, string> data, string passphrase, bool includeEmpty = false)
            => GenerateSignatureFromString(BuildParamString(data, passphrase, includeEmpty));

        private static string GenerateSignatureFromString(string paramString)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(paramString));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        // Exact replica of PHP urlencode(): only A-Za-z0-9 - _ . pass through;
        // space → +; everything else → %XX (uppercase, UTF-8 bytes).
        // .NET's WebUtility.UrlEncode() leaves ( ) ! * unencoded — PHP does not.
        private static string PhpUrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var sb = new StringBuilder(value.Length * 3);
            foreach (char c in value)
            {
                if (c == ' ')
                {
                    sb.Append('+');
                }
                else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                         (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.')
                {
                    sb.Append(c);
                }
                else
                {
                    foreach (var b in Encoding.UTF8.GetBytes(c.ToString()))
                        sb.Append('%').Append(b.ToString("X2"));
                }
            }
            return sb.ToString();
        }
    }
}
