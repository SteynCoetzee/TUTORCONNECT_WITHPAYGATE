using Microsoft.AspNetCore.Mvc;
using TutorConnect.API.Controllers;
using TutorConnect.API.DTOs;
using TutorConnect.API.Models;
using TutorConnect.Tests.Helpers;
using Xunit;

namespace TutorConnect.Tests
{
    public class LogHoursControllerTests
    {
        private (LogHoursController controller, TutorConnect.API.Data.AppDbContext ctx) Create(
            string dbName, int userId = 10, string role = "Tutor")
        {
            var ctx   = TestSetup.CreateContext(dbName);
            var audit = TestSetup.MakeAuditService(ctx);
            TestSetup.SeedRoles(ctx);
            ctx.Users.Add(TestSetup.MakeUser(userId, $"tutor{userId}@test.com", "P@ss1!", roleId: 2));
            ctx.SaveChanges();

            var controller = new LogHoursController(ctx, audit)
            {
                ControllerContext = TestSetup.MakeControllerContext(userId, role)
            };
            return (controller, ctx);
        }

        // ── LogTime validation ────────────────────────────────────────────────

        [Theory]
        [InlineData(0)]       // exactly zero
        [InlineData(-1)]      // negative
        [InlineData(25)]      // over 24 hours
        [InlineData(100)]
        public async Task LogTime_InvalidHours_ReturnsBadRequest(decimal hours)
        {
            var (controller, _) = Create($"LogHours_InvalidHours_{hours}");

            var result = await controller.LogTime(new LogHoursCreateDto
            {
                Log_Hours_Date   = DateOnly.FromDateTime(DateTime.Today),
                Log_Hours_Time   = new TimeOnly(9, 0),
                Log_Hours_Amount = hours,
                Tutor_ID         = 10
            });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Theory]
        [InlineData(0.5)]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(24)]
        public async Task LogTime_ValidHours_ReturnsOk(decimal hours)
        {
            var (controller, ctx) = Create($"LogHours_ValidHours_{hours}");

            var result = await controller.LogTime(new LogHoursCreateDto
            {
                Log_Hours_Date   = DateOnly.FromDateTime(DateTime.Today),
                Log_Hours_Time   = new TimeOnly(9, 0),
                Log_Hours_Amount = hours,
                Tutor_ID         = 10
            });

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task LogTime_ValidEntry_PersistedToDatabase()
        {
            var (controller, ctx) = Create("LogHours_Persisted");

            await controller.LogTime(new LogHoursCreateDto
            {
                Log_Hours_Date   = DateOnly.FromDateTime(DateTime.Today),
                Log_Hours_Time   = new TimeOnly(9, 0),
                Log_Hours_Amount = 2.5m,
                Tutor_ID         = 10
            });

            var saved = ctx.Log_Hours.FirstOrDefault(l => l.Tutor_ID == 10);
            Assert.NotNull(saved);
            Assert.Equal(2.5m, saved.Log_Hours_Amount);
        }

        // ── UpdateLogHours validation ─────────────────────────────────────────

        [Fact]
        public async Task UpdateLogHours_ZeroHours_ReturnsBadRequest()
        {
            var (controller, ctx) = Create("LogHours_UpdateZero");
            ctx.Log_Hours.Add(new Log_Hours
            {
                Log_Hours_ID     = 1,
                Log_Hours_Date   = DateOnly.FromDateTime(DateTime.Today),
                Log_Hours_Time   = new TimeOnly(9, 0),
                Log_Hours_Amount = 3m,
                Tutor_ID         = 10
            });
            await ctx.SaveChangesAsync();

            var result = await controller.UpdateLogHours(1, new LogHoursCreateDto
            {
                Log_Hours_Date   = DateOnly.FromDateTime(DateTime.Today),
                Log_Hours_Time   = new TimeOnly(9, 0),
                Log_Hours_Amount = 0m,
                Tutor_ID         = 10
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── ApproveLogHours reads adminId from token ──────────────────────────

        [Fact]
        public async Task ApproveLogHours_ReadsAdminIdFromToken_NotFromBody()
        {
            var (controller, ctx) = Create("LogHours_Approve", userId: 42, role: "Admin");

            ctx.Log_Hours.Add(new Log_Hours
            {
                Log_Hours_ID     = 5,
                Log_Hours_Date   = DateOnly.FromDateTime(DateTime.Today),
                Log_Hours_Time   = new TimeOnly(9, 0),
                Log_Hours_Amount = 2m,
                Tutor_ID         = 10
            });
            await ctx.SaveChangesAsync();

            await controller.ApproveLogHours(5);

            var log = await ctx.Log_Hours.FindAsync(5);
            Assert.True(log!.IsApproved);
            Assert.Equal(42, log.ApprovedBy_Admin_ID); // Must match token user (42), not a body-supplied value
        }
    }
}
