using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TutorConnect.API.DTOs
{
    // --- SIGN UP / SIGN IN (AuthController) ---

    // Request body for AuthController.Register
    public class UserRegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Range(1, 4)]
        public int RoleId { get; set; } = 3;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }
    }

    // Request body for AuthController.Login
    public class UserLoginDto
    {
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    // --- CORE ACCOUNT (UsersController) ---

    // Response shape for UsersController.GetUser/GetAllUsers — explicit [JsonPropertyName]
    // on every field keeps the wire format stable even if these C# property names change
    public class UserProfileDto
    {
        [JsonPropertyName("user_ID")]
        public int User_ID { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        [JsonPropertyName("user_Role_ID")]
        public int User_Role_ID { get; set; }

        [JsonPropertyName("roleName")]
        public string? RoleName { get; set; }

        [JsonPropertyName("is_Archived")]
        public bool Is_Archived { get; set; }

        [JsonPropertyName("is_Deleted")]
        public bool Is_Deleted { get; set; }

        [JsonPropertyName("profile_Picture_Url")]
        public string? Profile_Picture_Url { get; set; }
    }

    // Request body for UsersController.UpdateUser — self-service edit, no role field
    public class UserProfileUpdateDto
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }
    }

    // --- ROLE-SPECIFIC PROFILES (UsersController.UpdateStudentProfile/UpdateTutorProfile/UpdateAdminProfile) ---

    public class StudentProfileDto
    {
        [StringLength(20)]
        public string? Student_Number { get; set; }

        [StringLength(100)]
        public string? Faculty { get; set; }

        [Range(1, 10)]
        public int? Year_Of_Study { get; set; }

        [StringLength(200)]
        public string? Degree_Program { get; set; }
    }

    public class TutorProfileDto
    {
        [StringLength(20)]
        public string? Employee_Number { get; set; }

        [StringLength(500)]
        public string? Qualifications { get; set; }

        [StringLength(200)]
        public string? Specialization { get; set; }

        [Range(0, 60)]
        public int? Years_Of_Experience { get; set; }
    }

    public class AdminProfileDto
    {
        [StringLength(20)]
        public string? Staff_Number { get; set; }

        [StringLength(100)]
        public string? Job_Title { get; set; }
    }

    // --- ADMIN ACCOUNT MANAGEMENT (UsersController.AdminUpdateUser/ChangeUserRole) ---

    // Request body for UsersController.AdminUpdateUser — full profile fields plus role in one call
    public class AdminUserUpdateDto
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        [Range(1, 4)]
        public int RoleId { get; set; }
    }

    // --- PASSWORD RESET FLOW (AuthController — ForgotPassword → VerifyResetCode → ResetPassword, in order) ---

    // Request body for AuthController.ForgotPassword
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
    }

    // Request body for AuthController.ResetPassword (final step)
    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string ResetCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    // Request body for AuthController.VerifyResetCode (middle step)
    public class VerifyResetCodeDto
    {
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string ResetCode { get; set; } = string.Empty;
    }

    // Request body for UsersController.ChangeUserRole
    public class ChangeRoleDto
    {
        [Range(1, 4)]
        public int RoleId { get; set; }
    }

    // --- PASSWORD CHANGE (AuthController.ChangePassword — self-service, requires the current password) ---
    public class ChangePasswordDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Current password cannot exceed 100 characters.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
