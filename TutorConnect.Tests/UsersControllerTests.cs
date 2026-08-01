using Microsoft.AspNetCore.Mvc;
using TutorConnect.API.Controllers;
using TutorConnect.API.DTOs;
using TutorConnect.API.Models;
using TutorConnect.Tests.Helpers;
using Xunit;

namespace TutorConnect.Tests
{
    public class UsersControllerTests
    {
        private (UsersController controller, TutorConnect.API.Data.AppDbContext ctx) CreateController(string dbName, int callerUserId, string callerRole = "Student")
        {
            var ctx   = TestSetup.CreateContext(dbName);
            var audit = TestSetup.MakeAuditService(ctx);
            TestSetup.SeedRoles(ctx);

            var controller = new UsersController(ctx, audit)
            {
                ControllerContext = TestSetup.MakeControllerContext(callerUserId, callerRole)
            };
            return (controller, ctx);
        }

        // ── UpdateUser ownership ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateUser_Owner_CanUpdateOwnProfile()
        {
            var (controller, ctx) = CreateController("Users_OwnerUpdate", callerUserId: 10);
            ctx.Users.Add(TestSetup.MakeUser(10, "owner@test.com", "P@ss1!"));
            await ctx.SaveChangesAsync();

            var result = await controller.UpdateUser(10, new UserProfileUpdateDto
            {
                FirstName = "Updated", LastName = "Name"
            });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUser_DifferentUser_ReturnsForbid()
        {
            // Caller is user 5 trying to update user 99's profile
            var (controller, ctx) = CreateController("Users_ForbidUpdate", callerUserId: 5, callerRole: "Student");
            ctx.Users.Add(TestSetup.MakeUser(99, "victim@test.com", "P@ss1!"));
            await ctx.SaveChangesAsync();

            var result = await controller.UpdateUser(99, new UserProfileUpdateDto
            {
                FirstName = "Hacked", LastName = "Name"
            });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateUser_Admin_CanUpdateAnyProfile()
        {
            // Admin (id 1) updating a student (id 99)
            var (controller, ctx) = CreateController("Users_AdminUpdate", callerUserId: 1, callerRole: "Admin");
            ctx.Users.Add(TestSetup.MakeUser(99, "student@test.com", "P@ss1!"));
            await ctx.SaveChangesAsync();

            var result = await controller.UpdateUser(99, new UserProfileUpdateDto
            {
                FirstName = "AdminChanged", LastName = "Name"
            });

            Assert.IsType<OkObjectResult>(result);
        }

        // ── Archive self-protection ───────────────────────────────────────────

        [Fact]
        public async Task ArchiveUser_CannotArchiveOwnAccount_ReturnsBadRequest()
        {
            var (controller, ctx) = CreateController("Users_ArchiveSelf", callerUserId: 7, callerRole: "Admin");
            ctx.Users.Add(TestSetup.MakeUser(7, "admin@test.com", "P@ss1!", roleId: 1));
            await ctx.SaveChangesAsync();

            var result = await controller.ArchiveUser(7);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("archive your own", bad.Value!.ToString()!);
        }

        [Fact]
        public async Task ArchiveUser_OtherUser_SetsIsArchivedTrue()
        {
            var (controller, ctx) = CreateController("Users_ArchiveOther", callerUserId: 1, callerRole: "Admin");
            ctx.Users.Add(TestSetup.MakeUser(1, "admin@test.com", "P@ss1!", roleId: 1));
            ctx.Users.Add(TestSetup.MakeUser(50, "student@test.com", "P@ss1!"));
            await ctx.SaveChangesAsync();

            await controller.ArchiveUser(50);

            var dbUser = await ctx.Users.FindAsync(50);
            Assert.True(dbUser!.Is_Archived);
        }

        // ── Soft delete ───────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_SetsIsDeletedAndClearsIsArchived()
        {
            var (controller, ctx) = CreateController("Users_SoftDelete", callerUserId: 1, callerRole: "Admin");
            ctx.Users.Add(TestSetup.MakeUser(1, "admin@test.com", "P@ss1!", roleId: 1));
            var target = TestSetup.MakeUser(60, "target@test.com", "P@ss1!", isArchived: true);
            ctx.Users.Add(target);
            await ctx.SaveChangesAsync();

            await controller.DeleteUser(60);

            var dbUser = await ctx.Users.FindAsync(60);
            Assert.True(dbUser!.Is_Deleted);
            Assert.False(dbUser.Is_Archived);
        }

        // ── Restore deleted ───────────────────────────────────────────────────

        [Fact]
        public async Task RestoreDeleted_SetsIsDeletedToFalse()
        {
            var (controller, ctx) = CreateController("Users_RestoreDeleted", callerUserId: 1, callerRole: "Admin");
            ctx.Users.Add(TestSetup.MakeUser(70, "deleted@test.com", "P@ss1!", isDeleted: true));
            await ctx.SaveChangesAsync();

            await controller.RestoreDeleted(70);

            var dbUser = await ctx.Users.FindAsync(70);
            Assert.False(dbUser!.Is_Deleted);
        }

        // ── UpdateStudentProfile ownership ────────────────────────────────────

        [Fact]
        public async Task UpdateStudentProfile_DifferentUser_ReturnsForbid()
        {
            var (controller, ctx) = CreateController("Users_StudentProfileForbid", callerUserId: 5, callerRole: "Student");
            ctx.Users.Add(TestSetup.MakeUser(99, "other@test.com", "P@ss1!"));
            await ctx.SaveChangesAsync();

            var result = await controller.UpdateStudentProfile(99, new StudentProfileDto
            {
                Faculty = "Engineering"
            });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateTutorProfile_DifferentUser_ReturnsForbid()
        {
            var (controller, ctx) = CreateController("Users_TutorProfileForbid", callerUserId: 5, callerRole: "Tutor");

            var result = await controller.UpdateTutorProfile(99, new TutorProfileDto
            {
                Specialization = "Maths"
            });

            Assert.IsType<ForbidResult>(result);
        }
    }
}
