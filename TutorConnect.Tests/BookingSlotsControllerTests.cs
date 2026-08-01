using Microsoft.AspNetCore.Mvc;
using TutorConnect.API.Controllers;
using TutorConnect.API.DTOs;
using TutorConnect.API.Models;
using TutorConnect.Tests.Helpers;
using Xunit;

namespace TutorConnect.Tests
{
    public class BookingSlotsControllerTests
    {
        private (BookingSlotsController controller, TutorConnect.API.Data.AppDbContext ctx) Create(string dbName)
        {
            var ctx = TestSetup.CreateContext(dbName);
            TestSetup.SeedRoles(ctx);
            ctx.Users.Add(TestSetup.MakeUser(10, "tutor@test.com", "P@ss1!", roleId: 2));
            ctx.SaveChanges();

            var controller = new BookingSlotsController(ctx)
            {
                ControllerContext = TestSetup.MakeControllerContext(10, "Tutor")
            };
            return (controller, ctx);
        }

        private BookingSlotCreateDto FutureSlot(int daysFromNow = 1, int? capacity = 10) => new()
        {
            Slot_Date    = DateOnly.FromDateTime(DateTime.Today.AddDays(daysFromNow)),
            Slot_Time    = new TimeOnly(10, 0),
            Session_Type = "OneOnOne",
            Location     = "Room 1",
            Tutor_ID     = 10,
            Max_Capacity = capacity,
            Module_Code  = "COS301"
        };

        // ── Past-date rejection ───────────────────────────────────────────────

        [Fact]
        public async Task CreateSlot_Yesterday_ReturnsBadRequest()
        {
            var (controller, _) = Create("Slots_PastDate");
            var dto = FutureSlot();
            dto.Slot_Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

            var result = await controller.CreateSlot(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateSlot_Today_IsAllowed()
        {
            var (controller, _) = Create("Slots_Today");
            var dto = FutureSlot();
            dto.Slot_Date = DateOnly.FromDateTime(DateTime.Today);

            var result = await controller.CreateSlot(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        // ── Capacity validation ───────────────────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task CreateSlot_InvalidCapacity_ReturnsBadRequest(int capacity)
        {
            var (controller, _) = Create($"Slots_BadCap_{capacity}");
            var result = await controller.CreateSlot(FutureSlot(capacity: capacity));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateSlot_NullCapacity_IsAllowed()
        {
            var (controller, _) = Create("Slots_NullCap");
            var result = await controller.CreateSlot(FutureSlot(capacity: null));
            Assert.IsType<OkObjectResult>(result);
        }

        // ── Duplicate slot prevention ─────────────────────────────────────────

        [Fact]
        public async Task CreateSlot_DuplicateDateAndTime_ReturnsBadRequest()
        {
            var (controller, ctx) = Create("Slots_Duplicate");

            ctx.Booking_Slots.Add(new Booking_Slot
            {
                Booking_Slot_ID = 1,
                Slot_Date       = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
                Slot_Time       = new TimeOnly(10, 0),
                Session_Type    = "OneOnOne",
                Tutor_ID        = 10,
                Is_Booked       = false
            });
            await ctx.SaveChangesAsync();

            var dto = FutureSlot(daysFromNow: 3);
            dto.Slot_Time = new TimeOnly(10, 0);

            var result = await controller.CreateSlot(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateSlot_DifferentTime_IsAllowed()
        {
            var (controller, ctx) = Create("Slots_DifferentTime");

            ctx.Booking_Slots.Add(new Booking_Slot
            {
                Booking_Slot_ID = 1,
                Slot_Date       = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
                Slot_Time       = new TimeOnly(10, 0),
                Session_Type    = "OneOnOne",
                Tutor_ID        = 10,
                Is_Booked       = false
            });
            await ctx.SaveChangesAsync();

            var dto = FutureSlot(daysFromNow: 3);
            dto.Slot_Time = new TimeOnly(14, 0); // Different time — should be allowed

            var result = await controller.CreateSlot(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        // ── Valid slot creation ───────────────────────────────────────────────

        [Fact]
        public async Task CreateSlot_ValidFutureSlot_PersistedToDatabase()
        {
            var (controller, ctx) = Create("Slots_Persisted");

            await controller.CreateSlot(FutureSlot(daysFromNow: 5));

            var saved = ctx.Booking_Slots.FirstOrDefault(s => s.Tutor_ID == 10);
            Assert.NotNull(saved);
            Assert.False(saved.Is_Booked);
        }

        // ── UpdateSlot past-date guard ────────────────────────────────────────

        [Fact]
        public async Task UpdateSlot_MoveToPastDate_ReturnsBadRequest()
        {
            var (controller, ctx) = Create("Slots_UpdatePast");

            ctx.Booking_Slots.Add(new Booking_Slot
            {
                Booking_Slot_ID = 10,
                Slot_Date       = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                Slot_Time       = new TimeOnly(9, 0),
                Session_Type    = "Group",
                Tutor_ID        = 10,
                Is_Booked       = false
            });
            await ctx.SaveChangesAsync();

            var dto = FutureSlot();
            dto.Slot_Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)); // Move to past

            var result = await controller.UpdateSlot(10, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSlot_AlreadyBooked_ReturnsBadRequest()
        {
            var (controller, ctx) = Create("Slots_UpdateBooked");

            ctx.Booking_Slots.Add(new Booking_Slot
            {
                Booking_Slot_ID = 20,
                Slot_Date       = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                Slot_Time       = new TimeOnly(9, 0),
                Session_Type    = "Group",
                Tutor_ID        = 10,
                Is_Booked       = true // Already booked!
            });
            await ctx.SaveChangesAsync();

            var result = await controller.UpdateSlot(20, FutureSlot(daysFromNow: 3));

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
