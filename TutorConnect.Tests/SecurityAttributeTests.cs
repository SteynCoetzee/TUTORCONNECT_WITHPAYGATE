using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using TutorConnect.API.Controllers;
using Xunit;

namespace TutorConnect.Tests
{
    /// <summary>
    /// Verifies that security attributes are correctly applied to sensitive endpoints.
    /// These tests catch accidental removal of [Authorize] / [AllowAnonymous] during refactoring.
    /// </summary>
    public class SecurityAttributeTests
    {
        // ── PayFast AdminProcess ──────────────────────────────────────────────

        [Fact]
        public void PayFastController_AdminProcess_HasAdminAuthorize()
        {
            var method = typeof(PayFastController).GetMethod("AdminProcess");
            Assert.NotNull(method);

            var authorizeAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authorizeAttr);
            Assert.Equal("Admin", authorizeAttr!.Roles);
        }

        [Fact]
        public void PayFastController_AdminProcess_IsNotAllowAnonymous()
        {
            var method = typeof(PayFastController).GetMethod("AdminProcess");
            Assert.NotNull(method);

            var anonAttr = method!.GetCustomAttribute<AllowAnonymousAttribute>();
            Assert.Null(anonAttr); // Must NOT be anonymous
        }

        // ── PayFast Initiate requires authentication ───────────────────────────

        [Fact]
        public void PayFastController_Initiate_RequiresAuthentication()
        {
            var method = typeof(PayFastController).GetMethod("Initiate");
            Assert.NotNull(method);

            // Initiate has [Authorize] at method level
            var methodAuth = method!.GetCustomAttribute<AuthorizeAttribute>();
            // If not on method, check at class level
            var classAuth = typeof(PayFastController).GetCustomAttribute<AuthorizeAttribute>();

            var isProtected = methodAuth != null || classAuth != null;
            Assert.True(isProtected, "PayFast/Initiate must require authentication.");
        }

        // ── GetAllUsers is Admin-only ─────────────────────────────────────────

        [Fact]
        public void UsersController_GetAllUsers_RequiresAdminRole()
        {
            var method = typeof(UsersController).GetMethod("GetAllUsers");
            Assert.NotNull(method);

            var authorizeAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authorizeAttr);
            Assert.Equal("Admin", authorizeAttr!.Roles);
        }

        // ── Archive / Delete require Admin ────────────────────────────────────

        [Theory]
        [InlineData("ArchiveUser")]
        [InlineData("UnarchiveUser")]
        [InlineData("DeleteUser")]
        [InlineData("RestoreDeleted")]
        [InlineData("PermanentDeleteUser")]
        public void UsersController_AdminActions_RequireAdminRole(string methodName)
        {
            var method = typeof(UsersController).GetMethod(methodName);
            Assert.NotNull(method);

            var authorizeAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authorizeAttr);
            Assert.Equal("Admin", authorizeAttr!.Roles);
        }

        // ── ApproveLogHours is Admin-only ─────────────────────────────────────

        [Fact]
        public void LogHoursController_ApproveLogHours_RequiresAdminRole()
        {
            var method = typeof(LogHoursController).GetMethod("ApproveLogHours");
            Assert.NotNull(method);

            var authorizeAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authorizeAttr);
            Assert.Equal("Admin", authorizeAttr!.Roles);
        }

        // ── Controllers are Authorized at class level ─────────────────────────

        [Theory]
        [InlineData(typeof(UsersController))]
        [InlineData(typeof(LogHoursController))]
        [InlineData(typeof(BookingSlotsController))]
        [InlineData(typeof(PaymentsController))]
        public void SecureControllers_HaveClassLevelAuthorize(Type controllerType)
        {
            var attr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attr);
        }

        // ── Auth controller endpoints are NOT accidentally locked ─────────────

        [Fact]
        public void AuthController_Register_IsPublic()
        {
            var method = typeof(AuthController).GetMethod("Register");
            Assert.NotNull(method);

            // Should not have [Authorize] at method level (it's a public endpoint)
            var auth = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.Null(auth);
        }

        [Fact]
        public void AuthController_Login_IsPublic()
        {
            var method = typeof(AuthController).GetMethod("Login");
            Assert.NotNull(method);

            var auth = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.Null(auth);
        }
    }
}
