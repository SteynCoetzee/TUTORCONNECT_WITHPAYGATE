using Microsoft.AspNetCore.Mvc;
using TutorConnect.API.Controllers;
using TutorConnect.API.DTOs;
using TutorConnect.Tests.Helpers;
using Xunit;

namespace TutorConnect.Tests
{
    public class AuthControllerTests
    {
        private AuthController CreateController(string dbName)
        {
            var ctx    = TestSetup.CreateContext(dbName);
            var audit  = TestSetup.MakeAuditService(ctx);
            var config = TestSetup.CreateFakeJwtConfig();
            TestSetup.SeedRoles(ctx);
            // EmailService is not exercised by Register or Login paths under test — pass null
            return new AuthController(ctx, config, null!, audit);
        }

        // ── Register ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            var ctx   = TestSetup.CreateContext("Auth_DuplicateEmail");
            TestSetup.SeedRoles(ctx);
            ctx.Users.Add(TestSetup.MakeUser(1, "taken@test.com", "P@ssword1"));
            await ctx.SaveChangesAsync();

            var audit      = TestSetup.MakeAuditService(ctx);
            var controller = new AuthController(ctx, TestSetup.CreateFakeJwtConfig(), null!, audit);

            var result = await controller.Register(new UserRegisterDto
            {
                FirstName = "Jane", LastName = "Doe",
                Email = "taken@test.com", Password = "N3wP@ss!",
                RoleId = 3
            });

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("already exists", bad.Value!.ToString()!);
        }

        [Theory]
        [InlineData("short1!")]          // too short
        [InlineData("alllowercase1!")]   // no uppercase
        [InlineData("NoDigitHere!")]     // no digit
        [InlineData("NoSpecial123")]     // no special char
        public async Task Register_WeakPassword_ReturnsBadRequest(string badPassword)
        {
            var controller = CreateController($"Auth_WeakPw_{badPassword.GetHashCode()}");

            var result = await controller.Register(new UserRegisterDto
            {
                FirstName = "Jane", LastName = "Doe",
                Email = "new@test.com", Password = badPassword,
                RoleId = 3
            });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Register_ValidRequest_ReturnsOk()
        {
            var controller = CreateController("Auth_ValidRegister");

            var result = await controller.Register(new UserRegisterDto
            {
                FirstName = "Jane", LastName = "Doe",
                Email = "jane@test.com", Password = "StrongP@ss1!",
                RoleId = 3
            });

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task Register_TutorRole_AssignedAsAwTutor()
        {
            var ctx    = TestSetup.CreateContext("Auth_TutorRole");
            TestSetup.SeedRoles(ctx);
            var audit      = TestSetup.MakeAuditService(ctx);
            var controller = new AuthController(ctx, TestSetup.CreateFakeJwtConfig(), null!, audit);

            await controller.Register(new UserRegisterDto
            {
                FirstName = "Bob", LastName = "Smith",
                Email = "bob@test.com", Password = "StrongP@ss1!",
                RoleId = 2  // Requests Tutor
            });

            var saved = ctx.Users.First(u => u.Email == "bob@test.com");
            Assert.Equal(4, saved.User_Role_ID); // Must be stored as AW-Tutor (pending approval)
        }

        // ── Login ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_UserNotFound_ReturnsBadRequest()
        {
            var controller = CreateController("Auth_UserNotFound");
            var result = await controller.Login(new UserLoginDto
                { Email = "ghost@test.com", Password = "P@ssword1" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsBadRequest()
        {
            var ctx = TestSetup.CreateContext("Auth_WrongPw");
            TestSetup.SeedRoles(ctx);
            ctx.Users.Add(TestSetup.MakeUser(1, "user@test.com", "Correct@1"));
            await ctx.SaveChangesAsync();

            var audit      = TestSetup.MakeAuditService(ctx);
            var controller = new AuthController(ctx, TestSetup.CreateFakeJwtConfig(), null!, audit);

            var result = await controller.Login(new UserLoginDto
                { Email = "user@test.com", Password = "Wrong@1" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_DeletedUser_ReturnsUnauthorized()
        {
            var ctx = TestSetup.CreateContext("Auth_Deleted");
            TestSetup.SeedRoles(ctx);
            ctx.Users.Add(TestSetup.MakeUser(1, "deleted@test.com", "P@ssword1", isDeleted: true));
            await ctx.SaveChangesAsync();

            var audit      = TestSetup.MakeAuditService(ctx);
            var controller = new AuthController(ctx, TestSetup.CreateFakeJwtConfig(), null!, audit);

            var result = await controller.Login(new UserLoginDto
                { Email = "deleted@test.com", Password = "P@ssword1" });

            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_ArchivedUser_AutoRestoresAndReturnsToken()
        {
            var ctx = TestSetup.CreateContext("Auth_Archived");
            TestSetup.SeedRoles(ctx);
            ctx.Users.Add(TestSetup.MakeUser(1, "archived@test.com", "P@ssword1", isArchived: true));
            await ctx.SaveChangesAsync();

            var audit      = TestSetup.MakeAuditService(ctx);
            var controller = new AuthController(ctx, TestSetup.CreateFakeJwtConfig(), null!, audit);

            var result = await controller.Login(new UserLoginDto
                { Email = "archived@test.com", Password = "P@ssword1" });

            // Login succeeds
            Assert.IsType<OkObjectResult>(result.Result);

            // DB state: Is_Archived must be false after successful login
            var dbUser = await ctx.Users.FindAsync(1);
            Assert.False(dbUser!.Is_Archived);
        }

        // ── ResetPassword ─────────────────────────────────────────────────────

        [Theory]
        [InlineData("short1!")]        // too short
        [InlineData("NoSpecial123")]   // no special char
        [InlineData("alllowercase1!")] // no uppercase
        public async Task ResetPassword_WeakNewPassword_ReturnsBadRequest(string weakPw)
        {
            var ctx = TestSetup.CreateContext($"Auth_ResetWeak_{weakPw.GetHashCode()}");
            TestSetup.SeedRoles(ctx);
            var user = TestSetup.MakeUser(1, "user@test.com", "OldP@ss1");
            user.PasswordResetCode = "123456";
            user.PasswordResetCodeExpiration = DateTime.Now.AddMinutes(10);
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var audit      = TestSetup.MakeAuditService(ctx);
            var controller = new AuthController(ctx, TestSetup.CreateFakeJwtConfig(), null!, audit);

            var result = await controller.ResetPassword(new ResetPasswordDto
            {
                Email = "user@test.com",
                ResetCode = "123456",
                NewPassword = weakPw
            });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }
}
