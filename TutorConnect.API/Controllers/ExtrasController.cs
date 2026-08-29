using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnect.API.Data;
using TutorConnect.API.DTOs;
using TutorConnect.API.Models;
using TutorConnect.API.Services;

namespace TutorConnect.API.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // NOTIFICATIONS CONTROLLER
    //
    // Read a user's notification feed and mark items read, one at a time or all at once.
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Notifications/user/5 — newest first; caller must be that user or an Admin
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<NotificationReturnDto>>> GetUserNotifications(int userId)
        {
            // Ownership check: users may only read their own notifications
            var callerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (callerId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var notifications = await _context.Notifications
                .Where(n => n.User_ID == userId)
                .OrderByDescending(n => n.Date_Sent) // Newest first!
                .Select(n => new NotificationReturnDto
                {
                    Notification_ID = n.Notification_ID,
                    Message = n.Message,
                    Date_Sent = n.Date_Sent,
                    Is_Read = n.Is_Read
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // PUT: api/Notifications/{id}/read — mark a single notification read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound("Notification not found.");
            notification.Is_Read = true;
            await _context.SaveChangesAsync();
            return Ok("Notification marked as read.");
        }

        // PUT: api/Notifications/user/{userId}/read-all — "clear all" button; marks every unread notification read
        [HttpPut("user/{userId}/read-all")]
        public async Task<IActionResult> MarkAllRead(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.User_ID == userId && !n.Is_Read)
                .ToListAsync();
            foreach (var n in unread) n.Is_Read = true;
            await _context.SaveChangesAsync();
            return Ok("All notifications marked as read.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AUDIT LOG CONTROLLER
    //
    // Read-only — entries are written elsewhere via AuditService.LogAsync(...), never through this
    // controller. Admin-only, capped at the 200 most recent matching rows.
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AuditLogsController(AppDbContext context) { _context = context; }

        // GET: api/AuditLogs — optionally filter by ?userId= and/or ?type= (substring match on Transaction_Type)
        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int? userId = null, [FromQuery] string? type = null)
        {
            var query = _context.Audit_Logs.AsQueryable();
            if (userId.HasValue)  query = query.Where(a => a.User_ID == userId.Value);
            if (!string.IsNullOrEmpty(type)) query = query.Where(a => a.Transaction_Type.Contains(type));

            var logs = await query
                .OrderByDescending(a => a.Audit_Date)
                .ThenByDescending(a => a.Audit_Time)
                .Take(200)
                .ToListAsync();

            var userIds = logs.Select(l => l.User_ID).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => $"{u.FirstName} {u.LastName}");

            return Ok(logs.Select(l => new
            {
                l.Audit_Log_ID,
                l.Audit_Date,
                l.Audit_Time,
                l.User_ID,
                UserName         = users.TryGetValue(l.User_ID, out var name) ? name : $"User #{l.User_ID}",
                l.Transaction_Type,
                l.Critical_Data
            }));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MODULE WISHLIST CONTROLLER
    //
    // Students suggest modules they'd like added to the catalogue. Plain CRUD, no Update —
    // a student who wants to change their suggestion deletes it and submits a new one.
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class ModuleWishlistController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ModuleWishlistController(AppDbContext context) { _context = context; }

        // GET: api/ModuleWishlist — admin sees all items with student names
        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var items = await _context.Module_Wishlists
                .OrderByDescending(w => w.Date_Submitted)
                .ToListAsync();

            var studentIds = items.Select(w => w.Student_ID).Distinct().ToList();
            var students = await _context.Users
                .Where(u => studentIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => $"{u.FirstName} {u.LastName}");

            return Ok(items.Select(w => new
            {
                w.Wishlist_ID,
                w.Module_Code,
                w.Module_Name,
                w.Student_ID,
                w.Date_Submitted,
                StudentName = students.TryGetValue(w.Student_ID, out var name) ? name : $"Student #{w.Student_ID}"
            }));
        }

        // GET: api/ModuleWishlist/student/{studentId} — student sees their own items
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult> GetByStudent(int studentId)
        {
            var items = await _context.Module_Wishlists
                .Where(w => w.Student_ID == studentId)
                .OrderByDescending(w => w.Date_Submitted)
                .ToListAsync();

            return Ok(items);
        }

        // POST: api/ModuleWishlist
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] ModuleWishlistCreateDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Module_Code) || string.IsNullOrWhiteSpace(request.Module_Name))
                return BadRequest("Module code and name are required.");

            var item = new Module_Wishlist
            {
                Module_Code   = request.Module_Code.Trim().ToUpper(),
                Module_Name   = request.Module_Name.Trim(),
                Student_ID    = request.Student_ID,
                Date_Submitted = DateTime.UtcNow
            };

            _context.Module_Wishlists.Add(item);
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        // DELETE: api/ModuleWishlist/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Module_Wishlists.FindAsync(id);
            if (item == null) return NotFound("Wishlist item not found.");
            _context.Module_Wishlists.Remove(item);
            await _context.SaveChangesAsync();
            return Ok("Wishlist item removed.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BUSINESS RULES CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class BusinessRulesController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Min/Max mirror the frontend's RULE_META bounds — kept in sync by hand, same as the
        // frontend, since these are just sanity bounds, not sensitive config.
        private static readonly List<(string Name, decimal Default, string Description, decimal Min, decimal Max)> _defaults = new()
        {
            ("session_timeout_minutes", 30, "Minutes of inactivity before a user is automatically logged out", 1, 480),
            ("afk_warning_minutes", 2, "Minutes before the AFK sign-out that an \"Are you still there?\" warning is shown", 0, 60),
            ("password_reset_code_expiration_minutes", 15, "Minutes a password reset code stays valid after it is emailed to a user", 1, 1440),
            ("module_max_price_oneonone", 10000, "Highest price (R) a module's one-on-one session price is allowed to be set to", 0, 1000000),
            ("module_max_price_group", 10000, "Highest price (R) a module's group session price is allowed to be set to", 0, 1000000),
        };

        public BusinessRulesController(AppDbContext context) { _context = context; }

        // GET: api/BusinessRules — readable by all authenticated roles (AFK timeout applies to everyone)
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAll()
        {
            await EnsureDefaultsExistAsync();
            var rules = await _context.Business_Rules.ToListAsync();
            // Always display in the order rules are defined in _defaults, regardless of DB insertion order
            var displayOrder = _defaults.Select((d, i) => (d.Name, i)).ToDictionary(x => x.Name, x => x.i);
            rules = rules
                .OrderBy(r => displayOrder.TryGetValue(r.Rule_Name, out var idx) ? idx : int.MaxValue)
                .ToList();
            return Ok(rules.Select(r => new
            {
                r.Rule_ID,
                r.Rule_Name,
                r.Rule_Value,
                Description = _defaults.FirstOrDefault(d => d.Name == r.Rule_Name).Description ?? ""
            }));
        }

        // PUT: api/BusinessRules/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] BusinessRuleUpdateDto dto)
        {
            var rule = await _context.Business_Rules.FindAsync(id);
            if (rule == null) return NotFound("Rule not found.");

            var bounds = _defaults.FirstOrDefault(d => d.Name == rule.Rule_Name);
            if (bounds.Name != null && (dto.Rule_Value < bounds.Min || dto.Rule_Value > bounds.Max))
                return BadRequest($"Value must be between {bounds.Min} and {bounds.Max}.");

            // The AFK warning must fire strictly before the AFK sign-out, or it's meaningless —
            // keep the two rules mutually consistent no matter which one is saved.
            if (rule.Rule_Name == "afk_warning_minutes")
            {
                var timeout = await _context.Business_Rules.FirstOrDefaultAsync(r => r.Rule_Name == "session_timeout_minutes");
                if (timeout != null && dto.Rule_Value >= timeout.Rule_Value)
                    return BadRequest($"AFK Warning Popup must be less than the AFK Session Timeout (currently {timeout.Rule_Value} minutes).");
            }
            else if (rule.Rule_Name == "session_timeout_minutes")
            {
                var warning = await _context.Business_Rules.FirstOrDefaultAsync(r => r.Rule_Name == "afk_warning_minutes");
                if (warning != null && dto.Rule_Value <= warning.Rule_Value)
                    return BadRequest($"AFK Session Timeout must be greater than the AFK Warning Popup time (currently {warning.Rule_Value} minutes).");
            }

            rule.Rule_Value = dto.Rule_Value;
            await _context.SaveChangesAsync();
            return Ok(new { rule.Rule_ID, rule.Rule_Name, rule.Rule_Value });
        }

        private async Task EnsureDefaultsExistAsync()
        {
            var existing = await _context.Business_Rules.Select(r => r.Rule_Name).ToListAsync();
            foreach (var (name, def, _, _, _) in _defaults)
            {
                if (!existing.Contains(name))
                {
                    _context.Business_Rules.Add(new Business_Rule { Rule_Name = name, Rule_Value = def });
                }
            }
            await _context.SaveChangesAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ROLE NAV PERMISSIONS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class RoleNavPermissionsController : ControllerBase
    {
        // Admin is configurable too, but the one hardcoded super-admin account (seeded in
        // Program.cs, TutorConnect00@gmail.com) is always exempted from it client-side —
        // see HARDCODED_ADMIN_EMAIL in the frontend's shared/nav-config.ts.
        private static readonly string[] _roles = { "Admin", "Tutor", "Student" };

        private readonly AppDbContext _context;
        public RoleNavPermissionsController(AppDbContext context) { _context = context; }

        // GET: api/RoleNavPermissions — readable by all authenticated roles (Tutor/Student read their own list on load)
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAll()
        {
            await EnsureDefaultsExistAsync();
            var settings = await _context.Role_Nav_Settings.ToListAsync();
            return Ok(settings.Select(s => new
            {
                s.Role_Nav_Setting_ID,
                s.Role,
                HiddenItems = s.Hidden_Items.Split(',', StringSplitOptions.RemoveEmptyEntries)
            }));
        }

        // PUT: api/RoleNavPermissions/{role}
        [HttpPut("{role}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(string role, [FromBody] UpdateHiddenItemsDto dto)
        {
            if (!_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Role must be Admin, Tutor, or Student.");

            var setting = await _context.Role_Nav_Settings.FirstOrDefaultAsync(s => s.Role == role);
            if (setting == null)
            {
                setting = new Role_Nav_Setting { Role = role };
                _context.Role_Nav_Settings.Add(setting);
            }
            setting.Hidden_Items = string.Join(",", dto.HiddenItems ?? new List<string>());
            await _context.SaveChangesAsync();
            return Ok(new { setting.Role_Nav_Setting_ID, setting.Role, HiddenItems = dto.HiddenItems });
        }

        private async Task EnsureDefaultsExistAsync()
        {
            var existing = await _context.Role_Nav_Settings.Select(s => s.Role).ToListAsync();
            foreach (var role in _roles)
            {
                if (!existing.Contains(role))
                {
                    _context.Role_Nav_Settings.Add(new Role_Nav_Setting { Role = role, Hidden_Items = "" });
                }
            }
            await _context.SaveChangesAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // USER NAV PERMISSIONS CONTROLLER — per-user overrides on top of Role_Nav_Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class UserNavPermissionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserNavPermissionsController(AppDbContext context) { _context = context; }

        // GET: api/UserNavPermissions/{userId} — any authenticated user (each user checks their own override on load)
        [HttpGet("{userId}")]
        public async Task<ActionResult> Get(int userId)
        {
            var setting = await _context.User_Nav_Settings.FirstOrDefaultAsync(s => s.User_ID == userId);
            return Ok(new
            {
                userId,
                hasOverride = setting != null,
                hiddenItems = setting != null
                    ? setting.Hidden_Items.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    : Array.Empty<string>()
            });
        }

        // PUT: api/UserNavPermissions/{userId} — Admin only
        [HttpPut("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int userId, [FromBody] UpdateHiddenItemsDto dto)
        {
            var setting = await _context.User_Nav_Settings.FirstOrDefaultAsync(s => s.User_ID == userId);
            if (setting == null)
            {
                setting = new User_Nav_Setting { User_ID = userId };
                _context.User_Nav_Settings.Add(setting);
            }
            setting.Hidden_Items = string.Join(",", dto.HiddenItems ?? new List<string>());
            await _context.SaveChangesAsync();
            return Ok(new { setting.User_Nav_Setting_ID, userId, HiddenItems = dto.HiddenItems });
        }

        // DELETE: api/UserNavPermissions/{userId} — Admin only — revert this user to their role's default
        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int userId)
        {
            var setting = await _context.User_Nav_Settings.FirstOrDefaultAsync(s => s.User_ID == userId);
            if (setting != null)
            {
                _context.User_Nav_Settings.Remove(setting);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}
