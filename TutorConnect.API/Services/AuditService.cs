using TutorConnect.API.Data;
using TutorConnect.API.Models;

namespace TutorConnect.API.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context) { _context = context; }

        public async Task LogAsync(int userId, string transactionType, string criticalData = "")
        {
            _context.Audit_Logs.Add(new Audit_Log
            {
                Audit_Date       = DateOnly.FromDateTime(SastClock.Now),
                Audit_Time       = TimeOnly.FromDateTime(SastClock.Now),
                User_ID          = userId,
                Transaction_Type = transactionType,
                Critical_Data    = criticalData
            });
            await _context.SaveChangesAsync();
        }
    }
}
