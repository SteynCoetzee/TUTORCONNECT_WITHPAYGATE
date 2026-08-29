using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnect.API.Data;

namespace TutorConnect.API.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // REPORTS CONTROLLER
    //
    // Entirely read-only, Admin-only — every endpoint aggregates existing data
    // (Log_Hours, Payments, Tutor_Reviews, Bookings, Student_Modules, etc.) into
    // a report; nothing here writes to the database. Most accept optional
    // ?from=&to= date-range query params. Two flavours:
    //   - Summary reports        one row per group (e.g. one row per tutor, per month)
    //   - "...-detail" reports   the underlying transactions themselves, sorted
    //                            for a control-break layout (grouped visually by
    //                            tutor/month in the frontend, not by the API)
    // GetCustomReport at the bottom is the odd one out — a single flexible
    // endpoint driven by ?entity=&groupBy= instead of a fixed shape. The frontend
    // has a service method for it (report.service.ts) but no UI currently calls it.
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReportsController(AppDbContext context) { _context = context; }

        private static string MonthName(int m) =>
            new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" }[Math.Clamp(m,1,12)-1];

        // ── Tutor Hours (6.9 & 6.10) ─────────────────────────────────────────
        [HttpGet("tutor-hours")]
        public async Task<ActionResult> GetTutorHoursReport(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Log_Hours.AsQueryable();
            if (from.HasValue) query = query.Where(l => l.Log_Hours_Date >= DateOnly.FromDateTime(from.Value));
            if (to.HasValue)   query = query.Where(l => l.Log_Hours_Date <= DateOnly.FromDateTime(to.Value));

            var grouped = await query
                .GroupBy(l => l.Tutor_ID)
                .Select(g => new { TutorId = g.Key, TotalHoursWorked = g.Sum(h => h.Log_Hours_Amount) })
                .ToListAsync();

            var ids = grouped.Select(g => g.TutorId).ToList();
            var names = await _context.Users
                .Where(u => ids.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => u.FirstName + " " + u.LastName);

            return Ok(grouped.Select(g => new {
                TutorName = names.GetValueOrDefault(g.TutorId, $"Tutor {g.TutorId}"),
                TotalHoursWorked = g.TotalHoursWorked
            }).OrderByDescending(x => x.TotalHoursWorked));
        }

        // ── Monthly Income (6.5 & 6.6) ───────────────────────────────────────
        [HttpGet("monthly-income")]
        public async Task<ActionResult> GetMonthlyIncome(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Payments.Where(p => p.Payment_Status == "Paid");
            if (from.HasValue) query = query.Where(p => p.Payment_Date >= from.Value);
            if (to.HasValue)   query = query.Where(p => p.Payment_Date <= to.Value);

            var report = await query
                .GroupBy(p => new { p.Payment_Date.Year, p.Payment_Date.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, TotalIncome = g.Sum(p => p.Amount) })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();

            return Ok(report.Select(r => new {
                Period = $"{MonthName(r.Month)} {r.Year}",
                r.TotalIncome
            }));
        }

        // ── Tutor Ratings (6.1 & 6.2) ────────────────────────────────────────
        [HttpGet("tutor-ratings")]
        public async Task<ActionResult> GetTutorRatingsReport()
        {
            var grouped = await _context.Tutor_Reviews
                .GroupBy(r => r.Tutor_ID)
                .Select(g => new {
                    TutorId = g.Key,
                    AverageRating = Math.Round(g.Average(r => (double)r.Tutor_Rating), 1),
                    TotalReviews = g.Count()
                }).ToListAsync();

            var ids = grouped.Select(g => g.TutorId).ToList();
            var names = await _context.Users
                .Where(u => ids.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => u.FirstName + " " + u.LastName);

            return Ok(grouped.Select(g => new {
                TutorName = names.GetValueOrDefault(g.TutorId, $"Tutor {g.TutorId}"),
                g.AverageRating,
                g.TotalReviews
            }).OrderByDescending(x => x.AverageRating));
        }

        // ── Monthly Students (6.3 & 6.4) ─────────────────────────────────────
        [HttpGet("monthly-students")]
        public async Task<ActionResult> GetMonthlyStudentsReport(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Bookings.Include(b => b.Booking_Slot).AsQueryable();
            if (from.HasValue) query = query.Where(b => b.Booking_Slot!.Slot_Date >= DateOnly.FromDateTime(from.Value));
            if (to.HasValue)   query = query.Where(b => b.Booking_Slot!.Slot_Date <= DateOnly.FromDateTime(to.Value));

            var report = await query
                .GroupBy(b => new { b.Booking_Slot!.Slot_Date.Year, b.Booking_Slot.Slot_Date.Month })
                .Select(g => new {
                    g.Key.Year, g.Key.Month,
                    UniqueStudents = g.Select(b => b.Student_ID).Distinct().Count()
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();

            return Ok(report.Select(r => new {
                Period = $"{MonthName(r.Month)} {r.Year}",
                r.UniqueStudents
            }));
        }

        // ── Sessions (6.7 & 6.8) ─────────────────────────────────────────────
        [HttpGet("sessions")]
        public async Task<ActionResult> GetSessionsReport(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] string? sessionType = null)
        {
            var query = _context.Bookings.Include(b => b.Booking_Slot).AsQueryable();
            if (from.HasValue) query = query.Where(b => b.Booking_Slot!.Slot_Date >= DateOnly.FromDateTime(from.Value));
            if (to.HasValue)   query = query.Where(b => b.Booking_Slot!.Slot_Date <= DateOnly.FromDateTime(to.Value));
            if (!string.IsNullOrEmpty(sessionType) && sessionType != "All")
                query = query.Where(b => b.Booking_Slot!.Session_Type!.StartsWith(sessionType));

            var raw = await query.ToListAsync();

            var studentIds = raw.Select(b => b.Student_ID).Distinct().ToList();
            var students   = await _context.Users
                .Where(u => studentIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => u.FirstName + " " + u.LastName);

            return Ok(raw.Select(b => new {
                BookingId   = b.Booking_ID,
                StudentName = students.GetValueOrDefault(b.Student_ID, $"Student {b.Student_ID}"),
                SlotDate    = b.Booking_Slot?.Slot_Date.ToString("yyyy-MM-dd") ?? "",
                SlotTime    = b.Booking_Slot?.Slot_Time.ToString("HH:mm") ?? "",
                SessionType = b.Booking_Slot?.Session_Type ?? "",
                Location    = b.Booking_Slot?.Location ?? ""
            }).OrderBy(x => x.SlotDate).ThenBy(x => x.SlotTime));
        }

        // ── Tutor Hours Detail (transactional — control break by tutor) ───────
        [HttpGet("tutor-hours-detail")]
        public async Task<ActionResult> GetTutorHoursDetail(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Log_Hours.AsQueryable();
            if (from.HasValue) query = query.Where(l => l.Log_Hours_Date >= DateOnly.FromDateTime(from.Value));
            if (to.HasValue)   query = query.Where(l => l.Log_Hours_Date <= DateOnly.FromDateTime(to.Value));

            var logs = await query.ToListAsync();
            var ids  = logs.Select(l => l.Tutor_ID).Distinct().ToList();
            var names = await _context.Users
                .Where(u => ids.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => u.FirstName + " " + u.LastName);

            return Ok(logs.Select(l => new {
                TutorName  = names.GetValueOrDefault(l.Tutor_ID, $"Tutor {l.Tutor_ID}"),
                LogDate    = l.Log_Hours_Date.ToString("yyyy-MM-dd"),
                Hours      = l.Log_Hours_Amount,
                Status     = l.IsApproved ? "Approved" : "Pending"
            }).OrderBy(x => x.TutorName).ThenBy(x => x.LogDate));
        }

        // ── Revenue Detail (transactional — control break by month) ──────────
        [HttpGet("revenue-detail")]
        public async Task<ActionResult> GetRevenueDetail(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Payments.Where(p => p.Payment_Status == "Paid");
            if (from.HasValue) query = query.Where(p => p.Payment_Date >= from.Value);
            if (to.HasValue)   query = query.Where(p => p.Payment_Date <= to.Value);

            var payments = await query.ToListAsync();
            var studentIds = payments.Select(p => p.Student_ID).Distinct().ToList();
            var students   = await _context.Users
                .Where(u => studentIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => u.FirstName + " " + u.LastName);

            return Ok(payments.Select(p => new {
                Period      = $"{MonthName(p.Payment_Date.Month)} {p.Payment_Date.Year}",
                PaymentDate = p.Payment_Date.ToString("yyyy-MM-dd"),
                StudentName = students.GetValueOrDefault(p.Student_ID, $"Student {p.Student_ID}"),
                ModuleCode  = p.Module_Code ?? "—",
                Amount      = p.Amount,
                Reference   = p.Payment_Reference ?? "—"
            }).OrderBy(x => x.PaymentDate));
        }

        // ── Monthly Student Enrollments (simple list) ────────────────────────
        // Shows students who joined the system for the first time each month
        [HttpGet("monthly-student-enrollments")]
        public async Task<ActionResult> GetMonthlyStudentEnrollments()
        {
            // Get each student's very first enrollment date (= when they joined)
            var firstJoins = await _context.Student_Modules
                .Include(sm => sm.Student)
                .GroupBy(sm => sm.Student_ID)
                .Select(g => new {
                    FirstDate   = g.Min(sm => sm.Enrollment_Date),
                    Student     = g.First().Student
                })
                .ToListAsync();

            var result = firstJoins
                .GroupBy(x => new { x.FirstDate.Year, x.FirstDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new {
                    Month          = $"{MonthName(g.Key.Month)} {g.Key.Year}",
                    NewStudents    = string.Join(", ", g
                        .Select(x => x.Student != null
                            ? x.Student.FirstName + " " + x.Student.LastName
                            : "Unknown")
                        .OrderBy(n => n)),
                    Count          = g.Count()
                });

            return Ok(result);
        }

        // ── Module Catalogue (simple list) ────────────────────────────────────
        [HttpGet("module-catalogue")]
        public async Task<ActionResult> GetModuleCatalogue()
        {
            var modules = await _context.Modules.ToListAsync();

            // Count 1-on-1 and group enrolments separately per module
            var enrollments = await _context.Student_Modules
                .Where(sm => sm.IsActive)
                .Select(sm => new { sm.Module_Code, sm.Can_Book_OneOnOne, sm.Can_Book_Group })
                .ToListAsync();

            var oneOnOneCounts = enrollments
                .Where(sm => sm.Can_Book_OneOnOne)
                .GroupBy(sm => sm.Module_Code)
                .ToDictionary(g => g.Key, g => g.Count());

            var groupCounts = enrollments
                .Where(sm => sm.Can_Book_Group)
                .GroupBy(sm => sm.Module_Code)
                .ToDictionary(g => g.Key, g => g.Count());

            return Ok(modules.Select(m => new {
                ModuleCode         = m.Module_Code,
                ModuleName         = m.Module_Name,
                OneOnOneStudents   = oneOnOneCounts.GetValueOrDefault(m.Module_Code, 0),
                GroupStudents      = groupCounts.GetValueOrDefault(m.Module_Code, 0)
            }).OrderBy(m => m.ModuleCode));
        }

        // ── Popular Modules (6.11 & 6.12) ────────────────────────────────────
        [HttpGet("popular-modules")]
        public async Task<ActionResult> GetPopularModulesReport(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Announcements.AsQueryable();
            if (from.HasValue) query = query.Where(a => a.Date_Posted >= from.Value);
            if (to.HasValue)   query = query.Where(a => a.Date_Posted <= to.Value);

            var grouped = await query
                .GroupBy(a => a.Module_Code)
                .Select(g => new { ModuleCode = g.Key, AnnouncementCount = g.Count() })
                .OrderByDescending(r => r.AnnouncementCount)
                .ToListAsync();

            var codes = grouped.Select(g => g.ModuleCode).ToList();
            var modules = await _context.Modules
                .Where(m => codes.Contains(m.Module_Code))
                .ToDictionaryAsync(m => m.Module_Code, m => m.Module_Name);

            return Ok(grouped.Select(g => new {
                ModuleCode = g.ModuleCode,
                ModuleName = modules.GetValueOrDefault(g.ModuleCode, g.ModuleCode),
                g.AnnouncementCount
            }));
        }

        // ── Module Enrollments ────────────────────────────────────────────────
        [HttpGet("module-enrollments")]
        public async Task<ActionResult> GetModuleEnrollments(
            [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var query = _context.Student_Modules.Where(sm => sm.IsActive).AsQueryable();
            if (from.HasValue) query = query.Where(sm => sm.Enrollment_Date >= from.Value);
            if (to.HasValue)   query = query.Where(sm => sm.Enrollment_Date < to.Value.AddDays(1));

            var grouped = await query
                .GroupBy(sm => sm.Module_Code)
                .Select(g => new {
                    ModuleCode = g.Key,
                    EnrolledStudents = g.Count(),
                    EarliestEnrollment = g.Min(sm => sm.Enrollment_Date),
                    LatestEnrollment   = g.Max(sm => sm.Enrollment_Date)
                })
                .OrderByDescending(r => r.EnrolledStudents)
                .Take(8)
                .ToListAsync();

            var codes = grouped.Select(g => g.ModuleCode).ToList();
            var names = await _context.Modules
                .Where(m => codes.Contains(m.Module_Code))
                .ToDictionaryAsync(m => m.Module_Code, m => m.Module_Name);

            return Ok(grouped.Select(g => new {
                g.ModuleCode,
                ModuleName = names.GetValueOrDefault(g.ModuleCode, g.ModuleCode),
                g.EnrolledStudents,
                EarliestEnrollment = g.EarliestEnrollment.ToString("yyyy-MM-dd"),
                LatestEnrollment   = g.LatestEnrollment.ToString("yyyy-MM-dd")
            }));
        }

        // ── Custom Query ──────────────────────────────────────────────────────
        // Flexible report: ?entity=users|bookings|enrollments|payments, optionally ?groupBy=
        // to switch the grouping dimension per entity (see the switch below for exactly which
        // groupBy values each entity supports).
        [HttpGet("custom")]
        public async Task<ActionResult> GetCustomReport(
            [FromQuery] string entity  = "bookings",
            [FromQuery] string groupBy = "month",
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to   = null)
        {
            switch (entity.ToLower())
            {
                case "users":
                {
                    var data = await _context.Users
                        .Include(u => u.User_Role)
                        .GroupBy(u => u.User_Role!.User_Role_Name)
                        .Select(g => new { Category = g.Key ?? "Unknown", Count = g.Count() })
                        .ToListAsync();
                    return Ok(data);
                }
                case "bookings":
                {
                    var q = _context.Bookings.Include(b => b.Booking_Slot).AsQueryable();
                    if (from.HasValue) q = q.Where(b => b.Booking_Slot!.Slot_Date >= DateOnly.FromDateTime(from.Value));
                    if (to.HasValue)   q = q.Where(b => b.Booking_Slot!.Slot_Date <= DateOnly.FromDateTime(to.Value));
                    if (groupBy == "type")
                    {
                        var data = await q.GroupBy(b => b.Booking_Slot!.Session_Type)
                            .Select(g => new { Category = g.Key ?? "Unknown", Count = g.Count() })
                            .ToListAsync();
                        return Ok(data);
                    }
                    var byMonth = await q
                        .GroupBy(b => new { b.Booking_Slot!.Slot_Date.Year, b.Booking_Slot.Slot_Date.Month })
                        .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                        .OrderBy(r => r.Year).ThenBy(r => r.Month).ToListAsync();
                    return Ok(byMonth.Select(r => new { Category = $"{MonthName(r.Month)} {r.Year}", r.Count }));
                }
                case "enrollments":
                {
                    var q = _context.Student_Modules.AsQueryable();
                    if (from.HasValue) q = q.Where(e => e.Enrollment_Date >= from.Value);
                    if (to.HasValue)   q = q.Where(e => e.Enrollment_Date <= to.Value);
                    if (groupBy == "module")
                    {
                        var data = await q.GroupBy(e => e.Module_Code)
                            .Select(g => new { Category = g.Key, Count = g.Count() })
                            .OrderByDescending(x => x.Count).ToListAsync();
                        return Ok(data);
                    }
                    var byMonth = await q
                        .GroupBy(e => new { e.Enrollment_Date.Year, e.Enrollment_Date.Month })
                        .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                        .OrderBy(r => r.Year).ThenBy(r => r.Month).ToListAsync();
                    return Ok(byMonth.Select(r => new { Category = $"{MonthName(r.Month)} {r.Year}", r.Count }));
                }
                case "payments":
                {
                    var q = _context.Payments.AsQueryable();
                    if (from.HasValue) q = q.Where(p => p.Payment_Date >= from.Value);
                    if (to.HasValue)   q = q.Where(p => p.Payment_Date <= to.Value);
                    if (groupBy == "status")
                    {
                        var data = await q.GroupBy(p => p.Payment_Status)
                            .Select(g => new { Category = g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
                            .ToListAsync();
                        return Ok(data);
                    }
                    var byMonth = await q
                        .GroupBy(p => new { p.Payment_Date.Year, p.Payment_Date.Month })
                        .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Total = g.Sum(p => p.Amount) })
                        .OrderBy(r => r.Year).ThenBy(r => r.Month).ToListAsync();
                    return Ok(byMonth.Select(r => new { Category = $"{MonthName(r.Month)} {r.Year}", r.Count, r.Total }));
                }
                default:
                    return BadRequest("Unknown entity. Use: users, bookings, enrollments, payments");
            }
        }
    }
}
