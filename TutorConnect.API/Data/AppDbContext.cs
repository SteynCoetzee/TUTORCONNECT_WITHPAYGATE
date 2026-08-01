using Microsoft.EntityFrameworkCore;
using TutorConnect.API.Models;

namespace TutorConnect.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── User → User_Role ──────────────────────────────────────────────
            modelBuilder.Entity<User>()
                .HasOne(u => u.User_Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.User_Role_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Profile tables → User ─────────────────────────────────────────
            modelBuilder.Entity<Student_Profile>()
                .HasOne(s => s.User).WithMany()
                .HasForeignKey(s => s.User_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tutor_Profile>()
                .HasOne(t => t.User).WithMany()
                .HasForeignKey(t => t.User_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Admin_Profile>()
                .HasOne(a => a.User).WithMany()
                .HasForeignKey(a => a.User_ID)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Notification → User ───────────────────────────────────────────
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User).WithMany()
                .HasForeignKey(n => n.User_ID)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Module_Resource → Module ──────────────────────────────────────
            modelBuilder.Entity<Module_Resource>()
                .HasOne(mr => mr.Module).WithMany()
                .HasForeignKey(mr => mr.Module_Code)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Assignment → Module ───────────────────────────────────────────
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Module).WithMany()
                .HasForeignKey(a => a.Module_Code)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Assignment_Submission → Assignment, User ──────────────────────
            modelBuilder.Entity<Assignment_Submission>()
                .HasOne(s => s.Assignment).WithMany()
                .HasForeignKey(s => s.Assignment_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Assignment_Submission>()
                .HasOne(s => s.Student).WithMany()
                .HasForeignKey(s => s.Student_ID)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Quiz → Module ─────────────────────────────────────────────────
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Module).WithMany()
                .HasForeignKey(q => q.Module_Code)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Quiz_Question → Quiz ──────────────────────────────────────────
            modelBuilder.Entity<Quiz_Question>()
                .HasOne<Quiz>().WithMany()
                .HasForeignKey(q => q.Quiz_ID)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Quiz_Question_Option → Quiz_Question ──────────────────────────
            modelBuilder.Entity<Quiz_Question_Option>()
                .HasOne<Quiz_Question>().WithMany()
                .HasForeignKey(o => o.Question_ID)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Student_Quiz → Quiz, User ─────────────────────────────────────
            modelBuilder.Entity<Student_Quiz>()
                .HasOne(sq => sq.Quiz).WithMany()
                .HasForeignKey(sq => sq.Quiz_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student_Quiz>()
                .HasOne<User>().WithMany()
                .HasForeignKey(sq => sq.Student_ID)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Student_Quiz_Answer → Student_Quiz, Question, Option ──────────
            modelBuilder.Entity<Student_Quiz_Answer>()
                .HasOne<Student_Quiz>().WithMany()
                .HasForeignKey(a => a.Student_Quiz_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student_Quiz_Answer>()
                .HasOne<Quiz_Question>().WithMany()
                .HasForeignKey(a => a.Question_ID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Student_Quiz_Answer>()
                .HasOne<Quiz_Question_Option>().WithMany()
                .HasForeignKey(a => a.Option_ID)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Log_Hours → User ──────────────────────────────────────────────
            modelBuilder.Entity<Log_Hours>()
                .HasOne(l => l.Tutor).WithMany()
                .HasForeignKey(l => l.Tutor_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Booking_Slot → User (Tutor) ───────────────────────────────────
            modelBuilder.Entity<Booking_Slot>()
                .HasOne(s => s.Tutor).WithMany()
                .HasForeignKey(s => s.Tutor_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Booking → Booking_Slot, User (Student) ────────────────────────
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Booking_Slot).WithMany()
                .HasForeignKey(b => b.Booking_Slot_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Student).WithMany()
                .HasForeignKey(b => b.Student_ID)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Student_Module → Module, User ─────────────────────────────────
            modelBuilder.Entity<Student_Module>()
                .HasOne(sm => sm.Module).WithMany()
                .HasForeignKey(sm => sm.Module_Code)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student_Module>()
                .HasOne(sm => sm.Student).WithMany()
                .HasForeignKey(sm => sm.Student_ID)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Tutor_Module → Module, User ───────────────────────────────────
            modelBuilder.Entity<Tutor_Module>()
                .HasOne(tm => tm.Module).WithMany()
                .HasForeignKey(tm => tm.Module_Code)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tutor_Module>()
                .HasOne(tm => tm.Tutor).WithMany()
                .HasForeignKey(tm => tm.Tutor_ID)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Student_Unenrollment → Module, User ───────────────────────────
            modelBuilder.Entity<Student_Unenrollment>()
                .HasOne(su => su.Module).WithMany()
                .HasForeignKey(su => su.Module_Code)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student_Unenrollment>()
                .HasOne(su => su.Student).WithMany()
                .HasForeignKey(su => su.Student_ID)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Payment → Module, User ────────────────────────────────────────
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Module).WithMany()
                .HasForeignKey(p => p.Module_Code)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Student).WithMany()
                .HasForeignKey(p => p.Student_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── FAQ → FAQ_Category ────────────────────────────────────────────
            modelBuilder.Entity<FAQ>()
                .HasOne<FAQ_Category>().WithMany()
                .HasForeignKey(f => f.FAQ_Category_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Testimonial → User, Testimonial_Category ──────────────────────
            modelBuilder.Entity<Testimonial>()
                .HasOne<User>().WithMany()
                .HasForeignKey(t => t.Student_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Testimonial>()
                .HasOne<Testimonial_Category>().WithMany()
                .HasForeignKey(t => t.Testimonial_Category_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Tutor_Review → User (student + tutor) ────────────────────────
            modelBuilder.Entity<Tutor_Review>()
                .HasOne<User>().WithMany()
                .HasForeignKey(tr => tr.Student_ID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tutor_Review>()
                .HasOne<User>().WithMany()
                .HasForeignKey(tr => tr.Tutor_ID)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Session_Review → User ─────────────────────────────────────────
            modelBuilder.Entity<Session_Review>()
                .HasOne<User>().WithMany()
                .HasForeignKey(sr => sr.Student_ID)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Session_Attendance → Attendance_Status ────────────────────────
            modelBuilder.Entity<Session_Attendance>()
                .HasOne(sa => sa.Attendance_Status).WithMany()
                .HasForeignKey(sa => sa.Attendance_Status_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Student_Group_Allocation → Student_Group, User ────────────────
            modelBuilder.Entity<Student_Group_Allocation>()
                .HasOne(sga => sga.Student_Group).WithMany()
                .HasForeignKey(sga => sga.Student_Group_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student_Group_Allocation>()
                .HasOne<User>().WithMany()
                .HasForeignKey(sga => sga.Student_ID)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Module_Wishlist → User ────────────────────────────────────────
            modelBuilder.Entity<Module_Wishlist>()
                .HasOne<User>().WithMany()
                .HasForeignKey(mw => mw.Student_ID)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<User_Role> User_Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Module> Modules { get; set; } = null!;
        public DbSet<Student_Module> Student_Modules { get; set; } = null!;
        public DbSet<Tutor_Module> Tutor_Modules { get; set; } = null!;
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Assignment_Submission> Assignment_Submissions { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Module_Resource> Module_Resources { get; set; }
        public DbSet<Log_Hours> Log_Hours { get; set; }

        public DbSet<Tutor_Review> Tutor_Reviews { get; set; }
        public DbSet<Session_Review> Session_Reviews { get; set; }
        public DbSet<Testimonial_Category> Testimonial_Categories { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }

        public DbSet<FAQ_Category> FAQ_Categories { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<Media_Content> Media_Contents { get; set; }
        public DbSet<Help_Page> Help_Pages { get; set; }
        public DbSet<Help_Resource> Help_Resources { get; set; }

        public DbSet<Audit_Log> Audit_Logs { get; set; }

        public DbSet<Student_Quiz> Student_Quizzes { get; set; }
        public DbSet<Recorded_Session> Recorded_Sessions { get; set; }
        public DbSet<Attendance_Status> Attendance_Statuses { get; set; }
        public DbSet<Session_Attendance> Session_Attendances { get; set; }
        public DbSet<Booking_Slot> Booking_Slots { get; set; }
        public DbSet<Student_Group> Student_Groups { get; set; }
        public DbSet<Student_Group_Allocation> Student_Group_Allocations { get; set; }
        public DbSet<Resource_Type> Resource_Types { get; set; }
        public DbSet<Business_Rule> Business_Rules { get; set; }

        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Module_Wishlist> Module_Wishlists { get; set; }
        public DbSet<Student_Unenrollment> Student_Unenrollments { get; set; }
        public DbSet<Student_Profile> Student_Profiles { get; set; }
        public DbSet<Tutor_Profile> Tutor_Profiles { get; set; }
        public DbSet<Admin_Profile> Admin_Profiles { get; set; }
        public DbSet<Quiz_Question> Quiz_Questions { get; set; }
        public DbSet<Quiz_Question_Option> Quiz_Question_Options { get; set; }
        public DbSet<Student_Quiz_Answer> Student_Quiz_Answers { get; set; }
    }
}