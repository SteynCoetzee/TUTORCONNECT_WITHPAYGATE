using Microsoft.AspNetCore.Mvc;
using TutorConnect.API.Controllers;
using TutorConnect.API.DTOs;
using TutorConnect.Tests.Helpers;
using Xunit;

namespace TutorConnect.Tests
{
    public class PaymentsControllerTests
    {
        private PaymentsController CreateController(string dbName)
        {
            var ctx = TestSetup.CreateContext(dbName);
            TestSetup.SeedRoles(ctx);
            ctx.Users.Add(TestSetup.MakeUser(1, "student@test.com", "P@ss1!"));
            ctx.SaveChanges();

            return new PaymentsController(ctx)
            {
                ControllerContext = TestSetup.MakeControllerContext(1, "Student")
            };
        }

        // ── Amount validation ─────────────────────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-500)]
        public async Task MakePayment_ZeroOrNegativeAmount_ReturnsBadRequest(decimal amount)
        {
            var controller = CreateController($"Payments_BadAmount_{amount}");

            var result = await controller.MakePayment(new PaymentCreateDto
            {
                Amount      = amount,
                Student_ID  = 1,
                Module_Code = "COS301",
                Bank        = "FNB"
            });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(100)]
        [InlineData(5000)]
        public async Task MakePayment_ValidAmount_ReturnsOk(decimal amount)
        {
            var controller = CreateController($"Payments_ValidAmount_{amount}");

            var result = await controller.MakePayment(new PaymentCreateDto
            {
                Amount      = amount,
                Student_ID  = 1,
                Module_Code = "COS301",
                Bank        = "FNB"
            });

            Assert.IsType<OkObjectResult>(result.Result);
        }

        // ── Notification on payment ───────────────────────────────────────────

        [Fact]
        public async Task MakePayment_ValidPayment_CreatesNotificationForStudent()
        {
            var ctx = TestSetup.CreateContext("Payments_Notification");
            TestSetup.SeedRoles(ctx);
            ctx.Users.Add(TestSetup.MakeUser(5, "student5@test.com", "P@ss1!"));
            await ctx.SaveChangesAsync();

            var controller = new PaymentsController(ctx)
            {
                ControllerContext = TestSetup.MakeControllerContext(5, "Student")
            };

            await controller.MakePayment(new PaymentCreateDto
            {
                Amount      = 250m,
                Student_ID  = 5,
                Module_Code = "COS326",
                Bank        = "Standard Bank"
            });

            var notification = ctx.Notifications.FirstOrDefault(n => n.User_ID == 5);
            Assert.NotNull(notification);
            Assert.Contains("Pending", notification.Message);
        }

        [Fact]
        public async Task MakePayment_ValidPayment_StatusIsPending()
        {
            var ctx = TestSetup.CreateContext("Payments_StatusPending");
            TestSetup.SeedRoles(ctx);
            ctx.Users.Add(TestSetup.MakeUser(3, "student3@test.com", "P@ss1!"));
            await ctx.SaveChangesAsync();

            var controller = new PaymentsController(ctx)
            {
                ControllerContext = TestSetup.MakeControllerContext(3, "Student")
            };

            await controller.MakePayment(new PaymentCreateDto
            {
                Amount      = 100m,
                Student_ID  = 3,
                Module_Code = "MAT211",
                Bank        = "ABSA"
            });

            var payment = ctx.Payments.FirstOrDefault(p => p.Student_ID == 3);
            Assert.NotNull(payment);
            Assert.Equal("Pending", payment.Payment_Status);
        }
    }
}
