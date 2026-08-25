using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BCrypt.Net;
using ClosedXML.Excel;
using TutorConnect.API.Data;
using TutorConnect.API.DTOs;
using TutorConnect.API.Models;
using TutorConnect.API.Services;

namespace TutorConnect.API.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // AUTH CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly AuditService _audit;

        public AuthController(AppDbContext context, IConfiguration configuration, EmailService emailService, AuditService audit)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _audit = audit;
        }

        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(UserRegisterDto request)
        {
            // 1. Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("User already exists.");
            }

            // 2. Enforce password complexity
            if (!IsPasswordComplex(request.Password))
                return BadRequest("Password must be at least 8 characters and include an uppercase letter, a number, and a special character.");

            // 3. Hash the password securely
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Create the new user
            // New tutor registrations start as AW-Tutor (role 4) until admin promotes them
            int assignedRoleId = request.RoleId == 2 ? 4 : request.RoleId;

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = passwordHash,
                User_Role_ID = assignedRoleId,
                Phone = request.Phone,
                Address = request.Address,
                Bio = request.Bio
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(user.User_ID, "User Registered", $"Email: {user.Email}, Role: {assignedRoleId}");

            return Ok("User successfully registered.");
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginDto request)
        {
            var user = await _context.Users
                .Include(u => u.User_Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            // Generic message to prevent email enumeration
            if (user == null)
            {
                return BadRequest("Invalid email or password.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Invalid email or password.");
            }

            // Deleted accounts cannot log in
            if (user.Is_Deleted)
                return Unauthorized("This account has been removed. Please contact an administrator.");

            // Auto-restore archived accounts when the user successfully logs in
            if (user.Is_Archived)
            {
                user.Is_Archived = false;
                await _context.SaveChangesAsync();
            }

            string token = CreateToken(user);
            await _audit.LogAsync(user.User_ID, "User Login", $"Email: {user.Email}");
            return Ok(token);
        }

        [HttpPut("change-password/{userId}")]
        public async Task<ActionResult<string>> ChangePassword(int userId, ChangePasswordDto request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest("New passwords do not match.");

            if (!IsPasswordComplex(request.NewPassword))
                return BadRequest("Password must be at least 8 characters and include an uppercase letter, a number, and a special character.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Password updated successfully.");
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<object>> ForgotPassword(ForgotPasswordDto request)
        {
            var expirationMinutes = await _context.Business_Rules
                .Where(r => r.Rule_Name == "password_reset_code_expiration_minutes")
                .Select(r => (double?)r.Rule_Value)
                .FirstOrDefaultAsync() ?? 15;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return Ok(new { message = "If this email is registered, a reset code has been sent.", expiresInMinutes = expirationMinutes });
            }

            // Generate a 6-character alphanumeric reset code (uppercase letters + digits)
            const string otpChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rng = new Random();
            var resetCode = new string(Enumerable.Range(0, 6).Select(_ => otpChars[rng.Next(otpChars.Length)]).ToArray());

            user.PasswordResetCode = resetCode;
            user.PasswordResetCodeExpiration = DateTime.Now.AddMinutes(expirationMinutes);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await _emailService.SendResetCodeAsync(user.Email, resetCode);
            return Ok(new { message = "If this email is registered, a reset code has been sent.", expiresInMinutes = expirationMinutes });
        }

        [HttpPost("verify-reset-code")]
        public async Task<ActionResult> VerifyResetCode([FromBody] VerifyResetCodeDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null) return BadRequest("User not found.");
            if (!string.Equals(user.PasswordResetCode, request.ResetCode?.Trim(), StringComparison.OrdinalIgnoreCase))
                return BadRequest("Incorrect OTP. Please check your email and try again.");
            if (user.PasswordResetCodeExpiration == null || DateTime.Now > user.PasswordResetCodeExpiration)
                return BadRequest("OTP has expired. Please request a new one.");
            return Ok("OTP verified successfully.");
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<string>> ResetPassword(ResetPasswordDto request)
        {
            if (!IsPasswordComplex(request.NewPassword))
                return BadRequest("Password must be at least 8 characters and include an uppercase letter, a number, and a special character.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Validate reset code and expiration (case-insensitive)
            if (!string.Equals(user.PasswordResetCode, request.ResetCode?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid reset code.");
            }

            if (user.PasswordResetCodeExpiration == null || DateTime.Now > user.PasswordResetCodeExpiration)
            {
                return BadRequest("Reset code has expired.");
            }

            // Hash the new password and update
            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordHash = newPasswordHash;
            user.PasswordResetCode = null;
            user.PasswordResetCodeExpiration = null;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Password successfully reset.");
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.User_ID.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.User_Role?.User_Role_Name ?? "Student")
            };

            var keyString = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT:Key is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                issuer: issuer,
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        private static bool IsPasswordComplex(string pw) =>
            pw.Length >= 8 &&
            pw.Any(char.IsUpper) &&
            pw.Any(char.IsDigit) &&
            pw.Any(c => !char.IsLetterOrDigit(c));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // USERS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        public UsersController(AppDbContext context, AuditService audit) { _context = context; _audit = audit; }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserProfileDto>> GetUser(int id)
        {
            // Ownership check: students may only view their own profile; Admin can view any
            var callerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (callerId != id && !User.IsInRole("Admin") && !User.IsInRole("Tutor"))
                return Forbid();

            var user = await _context.Users
                .Include(u => u.User_Role)
                .FirstOrDefaultAsync(u => u.User_ID == id);

            if (user == null) return NotFound("User not found.");

            return Ok(new UserProfileDto
            {
                User_ID             = user.User_ID,
                FirstName           = user.FirstName,
                LastName            = user.LastName,
                Email               = user.Email,
                Phone               = user.Phone,
                Address             = user.Address,
                Bio                 = user.Bio,
                User_Role_ID        = user.User_Role_ID,
                RoleName            = user.User_Role?.User_Role_Name,
                Is_Archived         = user.Is_Archived,
                Is_Deleted          = user.Is_Deleted,
                Profile_Picture_Url = user.Profile_Picture_Url
            });
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserProfileUpdateDto request)
        {
            var callerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (callerId != id && !User.IsInRole("Admin")) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Phone = request.Phone;
            user.Address = request.Address;
            user.Bio = request.Bio;

            await _context.SaveChangesAsync();
            return Ok("Profile updated successfully.");
        }

        // GET: api/Users (Admin only - list all users)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetAllUsers()
        {
            var users = await _context.Users
                .Include(u => u.User_Role)
                .ToListAsync();

            return Ok(users.Select(u => new UserProfileDto
            {
                User_ID           = u.User_ID,
                FirstName         = u.FirstName,
                LastName          = u.LastName,
                Email             = u.Email,
                Phone             = u.Phone,
                Address           = u.Address,
                Bio               = u.Bio,
                User_Role_ID      = u.User_Role_ID,
                RoleName          = u.User_Role?.User_Role_Name,
                Is_Archived         = u.Is_Archived,
                Is_Deleted          = u.Is_Deleted,
                Profile_Picture_Url = u.Profile_Picture_Url
            }));
        }

        // PUT: api/Users/{id}/admin (Admin only - update full profile + role)
        [HttpPut("{id}/admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUpdateUser(int id, AdminUserUpdateDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            var roleExists = await _context.User_Roles.AnyAsync(r => r.User_Role_ID == request.RoleId);
            if (!roleExists) return BadRequest("Invalid role ID.");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Phone = request.Phone;
            user.Address = request.Address;
            user.Bio = request.Bio;
            user.User_Role_ID = request.RoleId;

            await _context.SaveChangesAsync();
            return Ok("User updated successfully.");
        }

        // PUT: api/Users/{id}/role (Admin only - change user role)
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            var roleExists = await _context.User_Roles.AnyAsync(r => r.User_Role_ID == request.RoleId);
            if (!roleExists) return BadRequest("Invalid role ID.");

            user.User_Role_ID = request.RoleId;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(id, "Role Changed", $"User_ID: {id}, New_Role_ID: {request.RoleId}");
            return Ok("User role updated successfully.");
        }

        // GET: api/Users/{id}/role-profile
        [HttpGet("{id}/role-profile")]
        public async Task<IActionResult> GetRoleProfile(int id)
        {
            var user = await _context.Users.Include(u => u.User_Role).FirstOrDefaultAsync(u => u.User_ID == id);
            if (user == null) return NotFound("User not found.");
            var role = user.User_Role?.User_Role_Name ?? "";

            if (role == "Student")
                return Ok(await _context.Student_Profiles.FirstOrDefaultAsync(p => p.User_ID == id));
            if (role == "Tutor" || role == "AW-Tutor")
                return Ok(await _context.Tutor_Profiles.FirstOrDefaultAsync(p => p.User_ID == id));
            if (role == "Admin")
                return Ok(await _context.Admin_Profiles.FirstOrDefaultAsync(p => p.User_ID == id));
            return Ok(null);
        }

        // PUT: api/Users/{id}/student-profile
        [HttpPut("{id}/student-profile")]
        public async Task<IActionResult> UpdateStudentProfile(int id, [FromBody] StudentProfileDto dto)
        {
            var callerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (callerId != id && !User.IsInRole("Admin")) return Forbid();
            var profile = await _context.Student_Profiles.FirstOrDefaultAsync(p => p.User_ID == id);
            if (profile == null)
                _context.Student_Profiles.Add(new Student_Profile {
                    User_ID = id, Student_Number = dto.Student_Number,
                    Faculty = dto.Faculty, Year_Of_Study = dto.Year_Of_Study, Degree_Program = dto.Degree_Program
                });
            else
            {
                profile.Student_Number = dto.Student_Number;
                profile.Faculty = dto.Faculty;
                profile.Year_Of_Study = dto.Year_Of_Study;
                profile.Degree_Program = dto.Degree_Program;
            }
            await _context.SaveChangesAsync();
            return Ok("Student profile updated.");
        }

        // PUT: api/Users/{id}/tutor-profile
        [HttpPut("{id}/tutor-profile")]
        public async Task<IActionResult> UpdateTutorProfile(int id, [FromBody] TutorProfileDto dto)
        {
            var callerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (callerId != id && !User.IsInRole("Admin")) return Forbid();
            var profile = await _context.Tutor_Profiles.FirstOrDefaultAsync(p => p.User_ID == id);
            if (profile == null)
                _context.Tutor_Profiles.Add(new Tutor_Profile {
                    User_ID = id, Employee_Number = dto.Employee_Number,
                    Qualifications = dto.Qualifications,
                    Specialization = dto.Specialization, Years_Of_Experience = dto.Years_Of_Experience
                });
            else
            {
                profile.Employee_Number = dto.Employee_Number;
                profile.Qualifications = dto.Qualifications;
                profile.Specialization = dto.Specialization;
                profile.Years_Of_Experience = dto.Years_Of_Experience;
            }
            await _context.SaveChangesAsync();
            return Ok("Tutor profile updated.");
        }

        // PUT: api/Users/{id}/admin-profile
        [HttpPut("{id}/admin-profile")]
        public async Task<IActionResult> UpdateAdminProfile(int id, [FromBody] AdminProfileDto dto)
        {
            var callerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (callerId != id && !User.IsInRole("Admin")) return Forbid();
            var profile = await _context.Admin_Profiles.FirstOrDefaultAsync(p => p.User_ID == id);
            if (profile == null)
                _context.Admin_Profiles.Add(new Admin_Profile { User_ID = id, Staff_Number = dto.Staff_Number, Job_Title = dto.Job_Title });
            else
            {
                profile.Staff_Number = dto.Staff_Number;
                profile.Job_Title = dto.Job_Title;
            }
            await _context.SaveChangesAsync();
            return Ok("Admin profile updated.");
        }

        // POST: api/Users/{id}/archive
        [HttpPost("{id}/archive")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ArchiveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (id == currentUserId) return BadRequest("You cannot archive your own account.");
            user.Is_Archived = true;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(currentUserId, "User Archived", $"User_ID: {id}, Email: {user.Email}");
            return Ok("User archived.");
        }

        // POST: api/Users/{id}/unarchive
        [HttpPost("{id}/unarchive")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnarchiveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");
            user.Is_Archived = false;
            await _context.SaveChangesAsync();
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _audit.LogAsync(currentUserId, "User Unarchived", $"User_ID: {id}, Email: {user.Email}");
            return Ok("User restored to active.");
        }

        // POST: api/Users/{id}/profile-picture
        [HttpPost("{id}/profile-picture")]
        [Authorize]
        public async Task<IActionResult> UploadProfilePicture(int id, IFormFile file, [FromServices] CloudinaryService cloudinary)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var isAdmin = User.IsInRole("Admin");
            if (currentUserId != id && !isAdmin) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");
            if (file == null || file.Length == 0) return BadRequest("No file provided.");

            var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowed.Contains(file.ContentType.ToLower()))
                return BadRequest("Only JPEG, PNG, WebP, or GIF images are allowed.");
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("Profile picture must be under 5 MB.");

            using var stream = file.OpenReadStream();
            var url = await cloudinary.UploadImageAsync(stream, file.FileName);
            user.Profile_Picture_Url = url;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(currentUserId, "Profile Picture Updated", $"User_ID: {id}");
            return Ok(url);
        }

        // Returns a block reason if this student must not be deleted right now, null if safe to delete.
        private async Task<string?> StudentDeletionBlockedAsync(int studentId)
        {
            var hasSessions = await _context.Student_Modules
                .AnyAsync(sm => sm.Student_ID == studentId && sm.IsActive &&
                                (sm.Sessions_Remaining_OneOnOne > 0 || sm.Sessions_Remaining_Group > 0));
            if (hasSessions)
                return "This student still has unused sessions. Please wait until all sessions have been used or refund them before deleting.";

            var now = DateTime.UtcNow;
            var hasPaidThisMonth = await _context.Payments
                .AnyAsync(p => p.Student_ID == studentId &&
                               p.Payment_Status == "Paid" &&
                               p.Payment_Date.Year  == now.Year &&
                               p.Payment_Date.Month == now.Month);
            if (hasPaidThisMonth)
                return $"This student has a payment recorded for {now:MMMM yyyy}. Deletion is blocked until the next calendar month.";

            return null;
        }

        // DELETE: api/Users/{id} — soft delete (moves to Deleted tab, fully restorable)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.User_Role)
                .FirstOrDefaultAsync(u => u.User_ID == id);
            if (user == null) return NotFound("User not found.");
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (id == currentUserId) return BadRequest("You cannot delete your own account.");

            if (user.User_Role?.User_Role_Name == "Student")
            {
                var block = await StudentDeletionBlockedAsync(id);
                if (block != null) return Conflict(block);
            }

            user.Is_Deleted = true;
            user.Is_Archived = false;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(currentUserId, "User Soft-Deleted", $"User_ID: {id}, Email: {user.Email}");
            return Ok("User moved to deleted.");
        }

        // POST: api/Users/{id}/restore-deleted
        [HttpPost("{id}/restore-deleted")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RestoreDeleted(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");
            user.Is_Deleted = false;
            await _context.SaveChangesAsync();
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _audit.LogAsync(currentUserId, "User Restored", $"User_ID: {id}, Email: {user.Email}");
            return Ok("User restored to active.");
        }

        // DELETE: api/Users/{id}/permanent — irreversible hard delete
        [HttpDelete("{id}/permanent")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PermanentDeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.User_Role)
                .FirstOrDefaultAsync(u => u.User_ID == id);
            if (user == null) return NotFound("User not found.");

            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (id == currentUserId)
                return BadRequest("You cannot permanently delete your own account.");

            var role = user.User_Role?.User_Role_Name ?? "";

            if (role == "Student")
            {
                var block = await StudentDeletionBlockedAsync(id);
                if (block != null) return Conflict(block);
            }

            try
            {
                // ── Always remove notifications ───────────────────────────────
                _context.Notifications.RemoveRange(
                    _context.Notifications.Where(n => n.User_ID == id));

                if (role == "Student")
                {
                    // Quiz answers must be removed before their parent Student_Quiz rows
                    var quizIds = await _context.Student_Quizzes
                        .Where(q => q.Student_ID == id)
                        .Select(q => q.Student_Quiz_ID).ToListAsync();
                    _context.Student_Quiz_Answers.RemoveRange(
                        _context.Student_Quiz_Answers.Where(a => quizIds.Contains(a.Student_Quiz_ID)));
                    _context.Student_Quizzes.RemoveRange(
                        _context.Student_Quizzes.Where(q => q.Student_ID == id));

                    // Assignment submissions (student's work is deleted with them)
                    _context.Assignment_Submissions.RemoveRange(
                        _context.Assignment_Submissions.Where(s => s.Student_ID == id));

                    // Enrollments & unenrollments
                    _context.Student_Modules.RemoveRange(
                        _context.Student_Modules.Where(s => s.Student_ID == id));
                    _context.Student_Unenrollments.RemoveRange(
                        _context.Student_Unenrollments.Where(s => s.Student_ID == id));

                    // Reviews written by this student
                    _context.Tutor_Reviews.RemoveRange(
                        _context.Tutor_Reviews.Where(r => r.Student_ID == id));
                    _context.Session_Reviews.RemoveRange(
                        _context.Session_Reviews.Where(r => r.Student_ID == id));

                    // Attendance, bookings, payments, wishlist, testimonials, group allocations
                    _context.Session_Attendances.RemoveRange(
                        _context.Session_Attendances.Where(a => a.Student_ID == id));
                    _context.Bookings.RemoveRange(
                        _context.Bookings.Where(b => b.Student_ID == id));
                    _context.Payments.RemoveRange(
                        _context.Payments.Where(p => p.Student_ID == id));
                    _context.Module_Wishlists.RemoveRange(
                        _context.Module_Wishlists.Where(w => w.Student_ID == id));
                    _context.Testimonials.RemoveRange(
                        _context.Testimonials.Where(t => t.Student_ID == id));
                    _context.Student_Group_Allocations.RemoveRange(
                        _context.Student_Group_Allocations.Where(g => g.Student_ID == id));

                    // Profile
                    var sp = await _context.Student_Profiles.FirstOrDefaultAsync(p => p.User_ID == id);
                    if (sp != null) _context.Student_Profiles.Remove(sp);
                }
                else if (role == "Tutor" || role == "AW-Tutor")
                {
                    // Tutor's uploaded content (modules, resources, assignments, quizzes) stays.
                    // Only remove the tutor's personal links and activity.

                    // Tutor-module assignments (link rows only, not the modules themselves)
                    _context.Tutor_Modules.RemoveRange(
                        _context.Tutor_Modules.Where(t => t.Tutor_ID == id));

                    // Reviews about this tutor
                    _context.Tutor_Reviews.RemoveRange(
                        _context.Tutor_Reviews.Where(r => r.Tutor_ID == id));

                    // Nullify graded_by on submissions so student work is preserved
                    var graded = await _context.Assignment_Submissions
                        .Where(s => s.Graded_By == id).ToListAsync();
                    graded.ForEach(s => s.Graded_By = null);

                    // Booking slots and their bookings
                    var slotIds = await _context.Booking_Slots
                        .Where(s => s.Tutor_ID == id)
                        .Select(s => s.Booking_Slot_ID).ToListAsync();
                    _context.Bookings.RemoveRange(
                        _context.Bookings.Where(b => slotIds.Contains(b.Booking_Slot_ID)));
                    _context.Booking_Slots.RemoveRange(
                        _context.Booking_Slots.Where(s => s.Tutor_ID == id));

                    // Log hours and announcements
                    _context.Log_Hours.RemoveRange(
                        _context.Log_Hours.Where(l => l.Tutor_ID == id));
                    _context.Announcements.RemoveRange(
                        _context.Announcements.Where(a => a.Tutor_ID == id));

                    // Profile
                    var tp = await _context.Tutor_Profiles.FirstOrDefaultAsync(p => p.User_ID == id);
                    if (tp != null) _context.Tutor_Profiles.Remove(tp);
                }
                else if (role == "Admin")
                {
                    // Nullify approved-by on log hours so tutor records are preserved
                    var approved = await _context.Log_Hours
                        .Where(l => l.ApprovedBy_Admin_ID == id).ToListAsync();
                    approved.ForEach(l => l.ApprovedBy_Admin_ID = null);

                    _context.Announcements.RemoveRange(
                        _context.Announcements.Where(a => a.Admin_ID == id));

                    var ap = await _context.Admin_Profiles.FirstOrDefaultAsync(p => p.User_ID == id);
                    if (ap != null) _context.Admin_Profiles.Remove(ap);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await _audit.LogAsync(currentUserId, "User Permanently Deleted", $"User_ID: {id}, Role: {role}, Email: {user.Email}");
                return Ok("User permanently deleted.");
            }
            catch (DbUpdateException ex)
            {
                return Conflict($"Permanent delete failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MODULES CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class ModulesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public ModulesController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // GET: api/Modules (Everyone can see available modules)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ModuleReturnDto>>> GetModules([FromQuery] int? studentId = null)
        {
            // If studentId is provided, return only enrolled modules for that student
            if (studentId.HasValue && studentId.Value > 0)
            {
                var enrolledModules = await _context.Student_Modules
                    .Where(sm => sm.Student_ID == studentId.Value && sm.IsActive)
                    .Include(sm => sm.Module)
                    .Select(sm => sm.Module)
                    .Select(m => new ModuleReturnDto
                    {
                        Module_Code = m!.Module_Code,
                        Module_Name = m.Module_Name,
                        Module_Description = m.Module_Description,
                        Module_Price_OneOnOne = m.Module_Price_OneOnOne,
                        Module_Price_Group = m.Module_Price_Group
                    })
                    .ToListAsync();

                return Ok(enrolledModules);
            }

            // Return all available modules (for browsing before enrollment)
            var modules = await _context.Modules
                .Select(m => new ModuleReturnDto
                {
                    Module_Code = m.Module_Code,
                    Module_Name = m.Module_Name,
                    Module_Description = m.Module_Description,
                    Module_Price_OneOnOne = m.Module_Price_OneOnOne,
                    Module_Price_Group = m.Module_Price_Group
                })
                .ToListAsync();

            return Ok(modules);
        }

        // Reads the admin-configurable price ceilings from Business_Rules (falls back to 10000 if not set yet).
        private async Task<(decimal MaxOneOnOne, decimal MaxGroup)> GetPriceCapsAsync()
        {
            var rules = await _context.Business_Rules
                .Where(r => r.Rule_Name == "module_max_price_oneonone" || r.Rule_Name == "module_max_price_group")
                .ToDictionaryAsync(r => r.Rule_Name, r => r.Rule_Value);
            var maxOneOnOne = rules.TryGetValue("module_max_price_oneonone", out var m1) ? m1 : 10000m;
            var maxGroup = rules.TryGetValue("module_max_price_group", out var m2) ? m2 : 10000m;
            return (maxOneOnOne, maxGroup);
        }

        // POST: api/Modules (4.1 Create module - Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Module>> CreateModule(ModuleCreateDto request)
        {
            if (await _context.Modules.AnyAsync(m => m.Module_Code == request.Module_Code))
            {
                return BadRequest("A module with this code already exists.");
            }

            var (maxOneOnOne, maxGroup) = await GetPriceCapsAsync();
            if (request.Module_Price_OneOnOne > maxOneOnOne)
                return BadRequest($"One-on-one price cannot exceed R{maxOneOnOne:N0}.");
            if (request.Module_Price_Group > maxGroup)
                return BadRequest($"Group price cannot exceed R{maxGroup:N0}.");

            var newModule = new Module
            {
                Module_Code = request.Module_Code,
                Module_Name = request.Module_Name,
                Module_Description = request.Module_Description,
                Module_Price_OneOnOne = request.Module_Price_OneOnOne,
                Module_Price_Group = request.Module_Price_Group
            };

            _context.Modules.Add(newModule);
            await _context.SaveChangesAsync();
            var creatorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _audit.LogAsync(creatorId, "Module Created", $"Module_Code: {newModule.Module_Code}, Name: {newModule.Module_Name}");

            return Ok("Module created successfully.");
        }

        // PUT: api/Modules/{code} (4.3 Update module - Admin only)
        [HttpPut("{code}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateModule(string code, ModuleCreateDto request)
        {
            var module = await _context.Modules.FindAsync(code);
            if (module == null) return NotFound("Module not found.");

            var (maxOneOnOne, maxGroup) = await GetPriceCapsAsync();
            if (request.Module_Price_OneOnOne > maxOneOnOne)
                return BadRequest($"One-on-one price cannot exceed R{maxOneOnOne:N0}.");
            if (request.Module_Price_Group > maxGroup)
                return BadRequest($"Group price cannot exceed R{maxGroup:N0}.");

            module.Module_Name = request.Module_Name;
            module.Module_Description = request.Module_Description;
            module.Module_Price_OneOnOne = request.Module_Price_OneOnOne;
            module.Module_Price_Group = request.Module_Price_Group;

            await _context.SaveChangesAsync();
            return Ok("Module updated successfully.");
        }

        // DELETE: api/Modules/{code} (4.4 Delete module - Admin only)
        [HttpDelete("{code}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteModule(string code)
        {
            var module = await _context.Modules.FindAsync(code);
            if (module == null) return NotFound("Module not found.");

            var enrolledCount = await _context.Student_Modules.CountAsync(sm => sm.Module_Code == code && sm.IsActive);
            var assignedCount = await _context.Tutor_Modules.CountAsync(tm => tm.Module_Code == code && tm.IsActive);
            if (enrolledCount > 0 || assignedCount > 0)
            {
                var parts = new List<string>();
                if (enrolledCount > 0) parts.Add($"{enrolledCount} student{(enrolledCount == 1 ? "" : "s")} enrolled");
                if (assignedCount > 0) parts.Add($"{assignedCount} tutor{(assignedCount == 1 ? "" : "s")} assigned");
                return Conflict($"This module can't be deleted because it has {string.Join(" and ", parts)}. Unenrol the student(s) and/or remove the tutor(s) first, then try again.");
            }

            try
            {
                _context.Modules.Remove(module);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("This module can't be deleted because it has related records (payments, past enrollments, or booking slots) tied to it.");
            }

            var deleterId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _audit.LogAsync(deleterId, "Module Deleted", $"Module_Code: {code}");
            return Ok("Module deleted successfully.");
        }

        // ─── BULK IMPORT ───────────────────────────────────────────────────────

        private const string BulkExampleCode = "EXAMPLE-CODE-DO-NOT-USE";

        // GET: api/Modules/bulk-template (downloads a blank Excel workbook to fill in)
        [HttpGet("bulk-template")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadBulkTemplate()
        {
            var (maxOneOnOne, maxGroup) = await GetPriceCapsAsync();
            var brandTeal = XLColor.FromArgb(0x5B, 0xBF, 0xBA);

            using var workbook = new XLWorkbook();

            // Sheet 1: Instructions
            var instructions = workbook.Worksheets.Add("Instructions");
            instructions.Cell(1, 1).Value = "Column";
            instructions.Cell(1, 2).Value = "Required?";
            instructions.Cell(1, 3).Value = "Description / Format / Example";
            var instrHeader = instructions.Range(1, 1, 1, 3);
            instrHeader.Style.Font.Bold = true;
            instrHeader.Style.Fill.BackgroundColor = brandTeal;
            instrHeader.Style.Font.FontColor = XLColor.White;

            var fieldDocs = new (string col, string required, string desc)[]
            {
                ("Module_Code", "Yes", "Unique code, 1-20 characters (letters, numbers, - and _). Must not already exist in the system."),
                ("Module_Name", "Yes", "Display name, up to 100 characters."),
                ("Module_Description", "No", "Up to 500 characters. Can be left blank."),
                ("Module_Price_OneOnOne", "Yes", $"Price per one-on-one session, e.g. 350. Numbers only, no currency symbol, 0 to {maxOneOnOne:N0}."),
                ("Module_Price_Group", "Yes", $"Price per group session, e.g. 150. Numbers only, no currency symbol, 0 to {maxGroup:N0}.")
            };
            int r = 2;
            foreach (var (col, required, desc) in fieldDocs)
            {
                instructions.Cell(r, 1).Value = col;
                instructions.Cell(r, 2).Value = required;
                instructions.Cell(r, 3).Value = desc;
                r++;
            }
            instructions.Cell(r + 1, 1).Value = "Fill in your modules on the \"Modules\" sheet — don't rename its columns.";
            instructions.Cell(r + 2, 1).Value = $"Row 2 of the Modules sheet is an example — overwrite or delete it. Don't use \"{BulkExampleCode}\" as a real code.";
            instructions.Column(1).Width = 26;
            instructions.Column(2).Width = 12;
            instructions.Column(3).Width = 75;
            instructions.SheetView.FreezeRows(1);

            // Sheet 2: Modules (the sheet the upload endpoint parses)
            var sheet = workbook.Worksheets.Add("Modules");
            string[] headers = { "Module_Code", "Module_Name", "Module_Description", "Module_Price_OneOnOne", "Module_Price_Group" };
            for (int c = 0; c < headers.Length; c++) sheet.Cell(1, c + 1).Value = headers[c];
            var headerRange = sheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = brandTeal;
            headerRange.Style.Font.FontColor = XLColor.White;

            // Keep Module_Code as text so numeric-looking codes (e.g. "007") aren't coerced to numbers.
            sheet.Column(1).Style.NumberFormat.Format = "@";

            sheet.Cell(2, 1).Value = BulkExampleCode;
            sheet.Cell(2, 2).Value = "Introduction to Computer Science";
            sheet.Cell(2, 3).Value = "Covers programming fundamentals, data structures, and algorithms.";
            sheet.Cell(2, 4).Value = 350;
            sheet.Cell(2, 5).Value = 150;
            sheet.Range(2, 1, 2, headers.Length).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFF, 0xF3, 0xCD);

            for (int c = 1; c <= headers.Length; c++) sheet.Column(c).Width = 28;
            sheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Module_Bulk_Import_Template.xlsx");
        }

        // POST: api/Modules/bulk (uploads a filled-in copy of the template, creates every valid row)
        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ModuleBulkImportResult>> BulkCreateModules(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file provided.");
            if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".xlsx")
                return BadRequest("Only .xlsx files are supported.");
            if (file.Length > 5 * 1024 * 1024) return BadRequest("File must be under 5 MB.");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            XLWorkbook workbook;
            try { workbook = new XLWorkbook(stream); }
            catch { return BadRequest("The uploaded file is not a valid Excel workbook."); }

            using (workbook)
            {
                var sheet = workbook.Worksheets.FirstOrDefault(
                    w => w.Name.Equals("Modules", StringComparison.OrdinalIgnoreCase));
                if (sheet == null)
                    return BadRequest("The uploaded file does not contain a sheet named 'Modules'. Please use the downloaded template.");

                var result = new ModuleBulkImportResult();
                var (maxOneOnOne, maxGroup) = await GetPriceCapsAsync();
                var existingCodes = new HashSet<string>(
                    await _context.Modules.Select(m => m.Module_Code).ToListAsync(),
                    StringComparer.OrdinalIgnoreCase);
                var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var toInsert = new List<Module>();

                var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
                for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                {
                    var row = sheet.Row(rowNum);
                    var codeRaw = row.Cell(1).GetString().Trim();
                    var nameRaw = row.Cell(2).GetString().Trim();
                    var descRaw = row.Cell(3).GetString().Trim();
                    var price1Raw = row.Cell(4).GetString().Trim();
                    var price2Raw = row.Cell(5).GetString().Trim();

                    // Silent skip: a truly empty spacer row (every cell blank)
                    if (codeRaw.Length == 0 && nameRaw.Length == 0 && descRaw.Length == 0
                        && price1Raw.Length == 0 && price2Raw.Length == 0) continue;

                    // Silent skip: untouched example row
                    if (codeRaw.Equals(BulkExampleCode, StringComparison.OrdinalIgnoreCase)) continue;

                    result.TotalRowsProcessed++;
                    var rowErrors = new List<ModuleBulkRowError>();

                    var dto = new ModuleCreateDto { Module_Code = codeRaw, Module_Name = nameRaw, Module_Description = descRaw };

                    if (!TryReadPrice(price1Raw, out var p1, out var err1))
                        rowErrors.Add(new ModuleBulkRowError { RowNumber = rowNum, Field = "Module_Price_OneOnOne", Message = err1! });
                    else if (p1 > maxOneOnOne)
                        rowErrors.Add(new ModuleBulkRowError { RowNumber = rowNum, Field = "Module_Price_OneOnOne", Message = $"Cannot exceed R{maxOneOnOne:N0}." });
                    else dto.Module_Price_OneOnOne = p1;

                    if (!TryReadPrice(price2Raw, out var p2, out var err2))
                        rowErrors.Add(new ModuleBulkRowError { RowNumber = rowNum, Field = "Module_Price_Group", Message = err2! });
                    else if (p2 > maxGroup)
                        rowErrors.Add(new ModuleBulkRowError { RowNumber = rowNum, Field = "Module_Price_Group", Message = $"Cannot exceed R{maxGroup:N0}." });
                    else dto.Module_Price_Group = p2;

                    var validationResults = new List<ValidationResult>();
                    Validator.TryValidateObject(dto, new ValidationContext(dto), validationResults, validateAllProperties: true);
                    foreach (var vr in validationResults)
                        rowErrors.Add(new ModuleBulkRowError { RowNumber = rowNum, Field = vr.MemberNames.FirstOrDefault() ?? "", Message = vr.ErrorMessage ?? "Invalid value." });

                    if (existingCodes.Contains(dto.Module_Code))
                        rowErrors.Add(new ModuleBulkRowError { RowNumber = rowNum, Field = "Module_Code", Message = "A module with this code already exists." });
                    else if (!seenInFile.Add(dto.Module_Code))
                        rowErrors.Add(new ModuleBulkRowError { RowNumber = rowNum, Field = "Module_Code", Message = "Duplicate Module_Code within this file." });

                    if (rowErrors.Count > 0) { result.Errors.AddRange(rowErrors); continue; }

                    toInsert.Add(new Module
                    {
                        Module_Code = dto.Module_Code,
                        Module_Name = dto.Module_Name,
                        Module_Description = dto.Module_Description,
                        Module_Price_OneOnOne = dto.Module_Price_OneOnOne,
                        Module_Price_Group = dto.Module_Price_Group
                    });
                }

                if (toInsert.Count > 0)
                {
                    _context.Modules.AddRange(toInsert);
                    try { await _context.SaveChangesAsync(); }
                    catch (DbUpdateException ex)
                    {
                        return Conflict($"Bulk import failed while saving: {ex.InnerException?.Message ?? ex.Message}. No modules from this file were created — please retry.");
                    }
                }

                result.SuccessCount = toInsert.Count;
                result.CreatedModuleCodes = toInsert.Select(m => m.Module_Code).ToList();

                var creatorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var codesForLog = result.CreatedModuleCodes.Take(10).ToList();
                var suffix = result.CreatedModuleCodes.Count > 10 ? $" and {result.CreatedModuleCodes.Count - 10} more" : "";
                await _audit.LogAsync(creatorId, "Bulk Module Import",
                    $"{result.SuccessCount} of {result.TotalRowsProcessed} modules created via bulk upload: {string.Join(", ", codesForLog)}{suffix}");

                return Ok(result);
            }
        }

        private static bool TryReadPrice(string raw, out decimal value, out string? error)
        {
            value = 0m; error = null;
            if (raw.Length == 0) { error = "This field is required and must be a number."; return false; }
            if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                error = $"'{raw}' is not a valid number.";
                return false;
            }
            return true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MODULE RESOURCES CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class ModuleResourcesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinary;

        public ModuleResourcesController(AppDbContext context, CloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Module_Resource>>> GetResources()
            => Ok(await _context.Module_Resources.ToListAsync());

        // GET: api/ModuleResources/module/{moduleCode}
        [HttpGet("module/{moduleCode}")]
        public async Task<ActionResult> GetResourcesByModule(string moduleCode)
        {
            var resources = await _context.Module_Resources
                .Where(r => r.Module_Code == moduleCode)
                .OrderBy(r => r.Folder_Name)
                .ThenBy(r => r.Date_Added)
                .ToListAsync();

            return Ok(resources.Select(r => new
            {
                r.Module_Resource_ID,
                r.Module_Resource_Name,
                r.Module_Resource_Type_ID,
                r.Resource_URL,
                r.Folder_Name,
                r.Is_Visible,
                r.Folder_Is_Visible,
                r.Date_Added,
                r.Module_Code
            }));
        }

        // GET: api/ModuleResources/{id}/preview — proxies file from Cloudinary for inline browser viewing
        [HttpGet("{id}/preview")]
        public async Task<IActionResult> PreviewResource(int id)
        {
            var resource = await _context.Module_Resources.FindAsync(id);
            if (resource == null || string.IsNullOrEmpty(resource.Resource_URL))
                return NotFound("Resource not found.");

            using var http = new HttpClient();
            var response = await http.GetAsync(resource.Resource_URL);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Could not fetch file from storage.");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = resource.Module_Resource_Type_ID switch
            {
                "PDF"   => "application/pdf",
                "Video" => response.Content.Headers.ContentType?.MediaType ?? "video/mp4",
                _       => "application/octet-stream"
            };
            var safeName = Uri.EscapeDataString(resource.Module_Resource_Name ?? "file");
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{safeName}\"";
            return File(bytes, contentType);
        }

        // POST: api/ModuleResources/upload — file upload (PDF / Doc / Video)
        [HttpPost("upload")]
        [Authorize(Roles = "Tutor,Admin")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 600_000_000)] // 600 MB
        public async Task<ActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file provided.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".mp4", ".webm", ".mov" };
            if (!allowed.Contains(ext)) return BadRequest("File type not supported.");

            using var stream = file.OpenReadStream();
            var fileName = $"{Guid.NewGuid()}{ext}";
            try
            {
                var isVideo = new[] { ".mp4", ".webm", ".mov" }.Contains(ext);
                var isPdf   = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx" }.Contains(ext);
                var url = isVideo ? await _cloudinary.UploadVideoAsync(stream, fileName)
                        : isPdf   ? await _cloudinary.UploadRawAsync(stream, fileName)
                        :           await _cloudinary.UploadImageAsync(stream, fileName);
                return Ok(url);
            }
            catch
            {
                return StatusCode(500, "File upload failed. Please try again.");
            }
        }

        // POST: api/ModuleResources
        [HttpPost]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult> CreateResource(ResourceCreateDto request)
        {
            // Inherit folder visibility from existing resources in the same folder
            var folderName = request.Folder_Name?.Trim() ?? string.Empty;
            bool folderVisible = true;
            var existingInFolder = await _context.Module_Resources
                .Where(r => r.Module_Code == request.Module_Code && r.Folder_Name == folderName)
                .FirstOrDefaultAsync();
            if (existingInFolder != null)
                folderVisible = existingInFolder.Folder_Is_Visible;

            var resource = new Module_Resource
            {
                Module_Resource_Name = request.Module_Resource_Name,
                Module_Resource_Type_ID = request.Module_Resource_Type_ID,
                Module_Code = request.Module_Code,
                Resource_URL = request.Resource_URL,
                Folder_Name = folderName,
                Is_Visible = false,
                Folder_Is_Visible = folderVisible,
                Date_Added = DateTime.UtcNow
            };
            _context.Module_Resources.Add(resource);
            await _context.SaveChangesAsync();
            return Ok("Resource added successfully.");
        }

        // PUT: api/ModuleResources/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> UpdateResource(int id, ResourceCreateDto request)
        {
            var resource = await _context.Module_Resources.FindAsync(id);
            if (resource == null) return NotFound("Resource not found.");
            resource.Module_Resource_Name = request.Module_Resource_Name;
            resource.Module_Resource_Type_ID = request.Module_Resource_Type_ID;
            resource.Resource_URL = request.Resource_URL;
            resource.Folder_Name = request.Folder_Name?.Trim() ?? string.Empty;
            await _context.SaveChangesAsync();
            return Ok("Resource updated.");
        }

        // PUT: api/ModuleResources/{id}/visibility  — toggles a single file's Is_Visible
        [HttpPut("{id}/visibility")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> ToggleVisibility(int id, [FromBody] ResourceVisibilityDto request)
        {
            var resource = await _context.Module_Resources.FindAsync(id);
            if (resource == null) return NotFound("Resource not found.");
            resource.Is_Visible = request.Is_Visible;
            await _context.SaveChangesAsync();
            return Ok("Visibility updated.");
        }

        // PUT: api/ModuleResources/folder-visibility
        // Hide folder: sets Folder_Is_Visible=false AND Is_Visible=false for every file in folder
        // Show folder: sets Folder_Is_Visible=true only — file Is_Visible states are untouched
        [HttpPut("folder-visibility")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> SetFolderVisibility([FromBody] FolderVisibilityDto request)
        {
            var resources = await _context.Module_Resources
                .Where(r => r.Module_Code == request.Module_Code && r.Folder_Name == request.Folder_Name)
                .ToListAsync();

            foreach (var r in resources)
            {
                r.Folder_Is_Visible = request.Is_Visible;
                if (!request.Is_Visible)
                    r.Is_Visible = false; // hide all files when hiding folder
            }

            await _context.SaveChangesAsync();
            return Ok("Folder visibility updated.");
        }

        // DELETE: api/ModuleResources/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _context.Module_Resources.FindAsync(id);
            if (resource == null) return NotFound("Resource not found.");
            _context.Module_Resources.Remove(resource);
            await _context.SaveChangesAsync();
            return Ok("Resource deleted.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LOG HOURS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class LogHoursController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public LogHoursController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // GET: api/LogHours — all records (admin view)
        [HttpGet]
        public async Task<ActionResult> GetLoggedHours()
        {
            var logs = await _context.Log_Hours
                .Include(l => l.Tutor)
                .OrderByDescending(l => l.Log_Hours_Date)
                .ToListAsync();

            var result = logs.Select(l => new
            {
                l.Log_Hours_ID,
                l.Log_Hours_Date,
                l.Log_Hours_Time,
                l.Log_Hours_Amount,
                l.Tutor_ID,
                TutorName = l.Tutor != null ? $"{l.Tutor.FirstName} {l.Tutor.LastName}" : "Unknown",
                l.IsApproved,
                l.ApprovalDate,
                l.ApprovedBy_Admin_ID
            });

            return Ok(result);
        }

        // GET: api/LogHours/tutor/{tutorId} — specific tutor's hours (3.2 View tutor hours)
        [HttpGet("tutor/{tutorId}")]
        public async Task<ActionResult<IEnumerable<Log_Hours>>> GetTutorHours(int tutorId)
        {
            var hours = await _context.Log_Hours
                .Where(l => l.Tutor_ID == tutorId)
                .OrderByDescending(l => l.Log_Hours_Date)
                .ToListAsync();
            return Ok(hours);
        }

        // POST: api/LogHours — create new log entry (3.1 Add tutor hours)
        [HttpPost]
        public async Task<ActionResult<Log_Hours>> LogTime(LogHoursCreateDto request)
        {
            if (request.Log_Hours_Amount <= 0 || request.Log_Hours_Amount > 24)
                return BadRequest("Hours must be between 0.01 and 24.");

            if (request.Log_Hours_Date > DateOnly.FromDateTime(DateTime.UtcNow))
                return BadRequest("Log date cannot be in the future.");

            var newLog = new Log_Hours
            {
                Log_Hours_Date = request.Log_Hours_Date,
                Log_Hours_Time = request.Log_Hours_Time,
                Log_Hours_Amount = request.Log_Hours_Amount,
                Tutor_ID = request.Tutor_ID
            };

            _context.Log_Hours.Add(newLog);
            await _context.SaveChangesAsync();

            return Ok("Tutor hours logged successfully.");
        }

        // PUT: api/LogHours/{id} — update log entry (3.3 Update tutor hours)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLogHours(int id, LogHoursCreateDto request)
        {
            if (request.Log_Hours_Amount <= 0 || request.Log_Hours_Amount > 24)
                return BadRequest("Hours must be between 0.01 and 24.");

            if (request.Log_Hours_Date > DateOnly.FromDateTime(DateTime.UtcNow))
                return BadRequest("Log date cannot be in the future.");

            var log = await _context.Log_Hours.FindAsync(id);
            if (log == null) return NotFound("Log entry not found.");

            log.Log_Hours_Date = request.Log_Hours_Date;
            log.Log_Hours_Time = request.Log_Hours_Time;
            log.Log_Hours_Amount = request.Log_Hours_Amount;

            await _context.SaveChangesAsync();
            return Ok("Log entry updated successfully.");
        }

        // DELETE: api/LogHours/{id} — delete log entry (3.4 Delete tutor hours)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLogHours(int id)
        {
            var log = await _context.Log_Hours.FindAsync(id);
            if (log == null) return NotFound("Log entry not found.");

            _context.Log_Hours.Remove(log);
            await _context.SaveChangesAsync();
            return Ok("Log entry deleted successfully.");
        }

        // GET: api/LogHours/pending — all unapproved entries with tutor name (admin)
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetPendingHours()
        {
            var logs = await _context.Log_Hours
                .Include(l => l.Tutor)
                .Where(l => !l.IsApproved)
                .OrderBy(l => l.Log_Hours_Date)
                .ToListAsync();

            var result = logs.Select(l => new
            {
                l.Log_Hours_ID,
                l.Log_Hours_Date,
                l.Log_Hours_Time,
                l.Log_Hours_Amount,
                l.Tutor_ID,
                TutorName = l.Tutor != null ? $"{l.Tutor.FirstName} {l.Tutor.LastName}" : "Unknown"
            });

            return Ok(result);
        }

        // PUT: api/LogHours/{id}/approve — approve a log entry (admin)
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveLogHours(int id)
        {
            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var log = await _context.Log_Hours.FindAsync(id);
            if (log == null) return NotFound("Log entry not found.");

            log.IsApproved = true;
            log.ApprovalDate = DateTime.UtcNow;
            log.ApprovedBy_Admin_ID = adminId;

            await _context.SaveChangesAsync();
            await _audit.LogAsync(adminId, "Hours Approved", $"Log_ID: {id}, Tutor_ID: {log.Tutor_ID}, Hours: {log.Log_Hours_Amount}");
            return Ok("Log entry approved.");
        }

        // DELETE: api/LogHours/{id}/reject — reject (delete) a log entry (admin)
        [HttpDelete("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectLogHours(int id)
        {
            var rejectingAdminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var log = await _context.Log_Hours.FindAsync(id);
            if (log == null) return NotFound("Log entry not found.");

            _context.Log_Hours.Remove(log);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(rejectingAdminId, "Hours Rejected", $"Log_ID: {id}, Tutor_ID: {log.Tutor_ID}");
            return Ok("Log entry rejected and removed.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TESTIMONIALS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class TestimonialsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TestimonialsController(AppDbContext context) { _context = context; }

        // GET: Categories - open to all (needed by submit form and public viewer)
        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<ActionResult> GetCategories()
            => Ok(await _context.Testimonial_Categories.ToListAsync());

        // GET: For the public website (Only shows Approved ones)
        [HttpGet("approved")]
        [AllowAnonymous]
        public async Task<ActionResult> GetApprovedTestimonials()
        {
            return Ok(await _context.Testimonials.Where(t => t.IsApproved == true).ToListAsync());
        }

        // GET: For the Admin Dashboard (Shows ones waiting for review)
        [HttpGet("pending")]
        public async Task<ActionResult> GetPendingTestimonials()
        {
            return Ok(await _context.Testimonials.Where(t => t.IsApproved == false).ToListAsync());
        }

        // POST: A student submits a new testimonial
        [HttpPost]
        public async Task<ActionResult> CreateTestimonial(TestimonialCreateDto request)
        {
            var testimonial = new Testimonial
            {
                Testimonial_Description = request.Description,
                Student_ID = request.Student_ID,
                Testimonial_Category_ID = request.Testimonial_Category_ID,
                IsApproved = false // ALWAYS false initially. Admins must review!
            };
            _context.Testimonials.Add(testimonial);
            await _context.SaveChangesAsync();
            return Ok("Testimonial submitted for admin approval.");
        }

        // GET: All testimonials for a student
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult> GetStudentTestimonials(int studentId)
            => Ok(await _context.Testimonials.Where(t => t.Student_ID == studentId).ToListAsync());

        // PUT: An Admin clicks "Approve"
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveTestimonial(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null) return NotFound("Testimonial not found.");

            testimonial.IsApproved = true;
            await _context.SaveChangesAsync();
            return Ok("Testimonial approved for public display.");
        }

        // PUT: Student updates their testimonial (2.11 Update Testimonial)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTestimonial(int id, TestimonialUpdateDto request)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null) return NotFound("Testimonial not found.");

            testimonial.Testimonial_Description = request.Description;
            testimonial.Testimonial_Category_ID = request.Testimonial_Category_ID;
            testimonial.IsApproved = false; // Reset approval after edit
            await _context.SaveChangesAsync();
            return Ok("Testimonial updated. Awaiting admin approval.");
        }

        // DELETE: Student deletes their testimonial (2.12 Delete Testimonial)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null) return NotFound("Testimonial not found.");

            _context.Testimonials.Remove(testimonial);
            await _context.SaveChangesAsync();
            return Ok("Testimonial deleted.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ADMIN CONTENT ITERATION 4 CONTROLLER
    // FAQs (UC 4.12-4.15) and Help Pages (UC 4.24-4.27)
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/AdminContent")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class AdminContentIteration4Controller : ControllerBase
    {
        private readonly AppDbContext _context;
        public AdminContentIteration4Controller(AppDbContext context)
        {
            _context = context;
        }

        // ─── FAQs ──────────────────────────────────────────────────────────────

        [HttpGet("faqs")]
        [AllowAnonymous]
        public async Task<ActionResult> GetFAQs()
        {
            var faqs = await _context.FAQs.ToListAsync();
            return Ok(faqs.Select(f => new {
                faq_ID           = f.FAQ_ID,
                question         = f.Question,
                answer           = f.Answer,
                faq_Category_ID  = f.FAQ_Category_ID,
                applicable_Pages = f.Applicable_Pages
            }));
        }

        [HttpPost("faqs")]
        public async Task<ActionResult> CreateFAQ(FAQCreateDto request)
        {
            var faq = new FAQ
            {
                Question = request.Question,
                Answer = request.Answer,
                FAQ_Category_ID = request.FAQ_Category_ID,
                Applicable_Pages = request.Applicable_Pages
            };
            _context.FAQs.Add(faq);
            await _context.SaveChangesAsync();
            return Ok("FAQ added successfully.");
        }

        [HttpPut("faqs/{id}")]
        public async Task<IActionResult> UpdateFAQ(int id, FAQUpdateDto request)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq == null) return NotFound();
            faq.Question = request.Question;
            faq.Answer = request.Answer;
            faq.FAQ_Category_ID = request.FAQ_Category_ID;
            faq.Applicable_Pages = request.Applicable_Pages;
            await _context.SaveChangesAsync();
            return Ok("FAQ updated.");
        }

        [HttpDelete("faqs/{id}")]
        public async Task<IActionResult> DeleteFAQ(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq == null) return NotFound();
            _context.FAQs.Remove(faq);
            await _context.SaveChangesAsync();
            return Ok("FAQ deleted.");
        }

        // ─── HELP PAGES ────────────────────────────────────────────────────────

        [HttpGet("help-pages")]
        [AllowAnonymous]
        public async Task<ActionResult> GetHelpPages()
            => Ok(await _context.Help_Pages.ToListAsync());

        [HttpPost("help-pages")]
        public async Task<ActionResult> CreateHelpPage(HelpPageCreateDto request)
        {
            var page = new Help_Page
            {
                Help_Page_Title = request.Help_Page_Title,
                Help_Page_Description = request.Help_Page_Description
            };
            _context.Help_Pages.Add(page);
            await _context.SaveChangesAsync();
            return Ok("Help page created.");
        }

        [HttpPut("help-pages/{id}")]
        public async Task<IActionResult> UpdateHelpPage(int id, HelpPageCreateDto request)
        {
            var page = await _context.Help_Pages.FindAsync(id);
            if (page == null) return NotFound();
            page.Help_Page_Title = request.Help_Page_Title;
            page.Help_Page_Description = request.Help_Page_Description;
            await _context.SaveChangesAsync();
            return Ok("Help page updated.");
        }

        [HttpDelete("help-pages/{id}")]
        public async Task<IActionResult> DeleteHelpPage(int id)
        {
            var page = await _context.Help_Pages.FindAsync(id);
            if (page == null) return NotFound();
            _context.Help_Pages.Remove(page);
            await _context.SaveChangesAsync();
            return Ok("Help page deleted.");
        }
    }
}
