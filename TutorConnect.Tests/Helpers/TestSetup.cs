using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using TutorConnect.API.Data;
using TutorConnect.API.Models;
using TutorConnect.API.Services;

namespace TutorConnect.Tests.Helpers
{
    public static class TestSetup
    {
        /// <summary>Creates a fresh in-memory AppDbContext with a unique database name per test.</summary>
        public static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        /// <summary>Seeds the four standard roles so FK constraints are satisfied.</summary>
        public static void SeedRoles(AppDbContext ctx)
        {
            ctx.User_Roles.AddRange(
                new User_Role { User_Role_ID = 1, User_Role_Name = "Admin" },
                new User_Role { User_Role_ID = 2, User_Role_Name = "Tutor" },
                new User_Role { User_Role_ID = 3, User_Role_Name = "Student" },
                new User_Role { User_Role_ID = 4, User_Role_Name = "AW-Tutor" }
            );
            ctx.SaveChanges();
        }

        /// <summary>Creates a user with a BCrypt-hashed password ready to be added to the DB.</summary>
        public static User MakeUser(int id, string email, string plainPassword,
            int roleId = 3, bool isDeleted = false, bool isArchived = false)
        {
            return new User
            {
                User_ID       = id,
                FirstName     = "Test",
                LastName      = "User",
                Email         = email,
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                User_Role_ID  = roleId,
                Is_Deleted    = isDeleted,
                Is_Archived   = isArchived
            };
        }

        /// <summary>Builds a ControllerContext whose ClaimsPrincipal matches the given user id and role.</summary>
        public static ControllerContext MakeControllerContext(int userId, string role = "Student")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, "TestUser")
            };
            var identity  = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        /// <summary>Creates a minimal IConfiguration with fake JWT settings (safe for unit tests).</summary>
        public static IConfiguration CreateFakeJwtConfig()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"]    = "test-secret-key-must-be-at-least-64-characters-long-for-hmac-sha512!!",
                    ["Jwt:Issuer"] = "TutorConnectTestIssuer"
                })
                .Build();
        }

        /// <summary>Creates an AuditService backed by the same in-memory context (safe for tests).</summary>
        public static AuditService MakeAuditService(AppDbContext ctx) => new AuditService(ctx);
    }
}
