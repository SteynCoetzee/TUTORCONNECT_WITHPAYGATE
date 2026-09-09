using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFullReferentialIntegrity : Migration
    {
        // Conditional guards for the shadow-FK/index/column cleanup below: this migration was
        // scaffolded against a database that had accumulated EF-generated "shadow" duplicate
        // relationships (the "_ID1"/"Module_Code1"/"StudentUser_ID"-style objects — created back
        // when some model relationships were briefly ambiguous). On a database built by replaying
        // every migration from scratch, those shadow objects were never actually created by any
        // earlier migration, so unconditionally dropping them fails immediately. These helpers make
        // every drop a no-op where the object never existed, while still dropping it where it does
        // (e.g. on a machine that evolved through the ambiguous-model period and really has it).
        private static void DropForeignKeyIfExists(MigrationBuilder migrationBuilder, string name, string table) =>
            migrationBuilder.Sql($"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'{name}') ALTER TABLE [{table}] DROP CONSTRAINT [{name}];");

        private static void DropIndexIfExists(MigrationBuilder migrationBuilder, string name, string table) =>
            migrationBuilder.Sql($"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{name}' AND object_id = OBJECT_ID(N'[dbo].[{table}]')) DROP INDEX [{name}] ON [{table}];");

        private static void DropColumnIfExists(MigrationBuilder migrationBuilder, string name, string table) =>
            migrationBuilder.Sql($"IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[{table}]') AND name = N'{name}') ALTER TABLE [{table}] DROP COLUMN [{name}];");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DropForeignKeyIfExists(migrationBuilder, "FK_Admin_Profiles_Users_User_ID1", "Admin_Profiles");
            DropForeignKeyIfExists(migrationBuilder, "FK_Assignment_Submissions_Users_Student_ID", "Assignment_Submissions");
            DropForeignKeyIfExists(migrationBuilder, "FK_Assignments_Modules_Module_Code1", "Assignments");
            DropForeignKeyIfExists(migrationBuilder, "FK_Booking_Slots_Users_Tutor_ID", "Booking_Slots");
            DropForeignKeyIfExists(migrationBuilder, "FK_Bookings_Users_StudentUser_ID", "Bookings");
            DropForeignKeyIfExists(migrationBuilder, "FK_Log_Hours_Users_Tutor_ID", "Log_Hours");
            DropForeignKeyIfExists(migrationBuilder, "FK_Module_Resources_Modules_Module_Code1", "Module_Resources");
            DropForeignKeyIfExists(migrationBuilder, "FK_Notifications_Users_User_ID1", "Notifications");
            DropForeignKeyIfExists(migrationBuilder, "FK_Payments_Modules_Module_Code1", "Payments");
            DropForeignKeyIfExists(migrationBuilder, "FK_Payments_Users_StudentUser_ID", "Payments");
            DropForeignKeyIfExists(migrationBuilder, "FK_Quizzes_Modules_Module_Code1", "Quizzes");
            DropForeignKeyIfExists(migrationBuilder, "FK_Session_Attendances_Attendance_Statuses_Attendance_Status_ID1", "Session_Attendances");
            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Group_Allocations_Student_Groups_Student_Group_ID1", "Student_Group_Allocations");
            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Modules_Users_Student_ID", "Student_Modules");
            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Profiles_Users_User_ID1", "Student_Profiles");
            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Quizzes_Quizzes_Quiz_ID1", "Student_Quizzes");
            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Unenrollments_Modules_Module_Code", "Student_Unenrollments");
            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Unenrollments_Users_StudentUser_ID", "Student_Unenrollments");
            DropForeignKeyIfExists(migrationBuilder, "FK_Tutor_Modules_Users_Tutor_ID", "Tutor_Modules");
            DropForeignKeyIfExists(migrationBuilder, "FK_Tutor_Profiles_Users_User_ID1", "Tutor_Profiles");
            DropForeignKeyIfExists(migrationBuilder, "FK_Users_User_Roles_User_Role_ID", "Users");

            DropIndexIfExists(migrationBuilder, "IX_Tutor_Profiles_User_ID1", "Tutor_Profiles");
            DropIndexIfExists(migrationBuilder, "IX_Student_Unenrollments_StudentUser_ID", "Student_Unenrollments");
            DropIndexIfExists(migrationBuilder, "IX_Student_Quizzes_Quiz_ID1", "Student_Quizzes");
            DropIndexIfExists(migrationBuilder, "IX_Student_Profiles_User_ID1", "Student_Profiles");
            DropIndexIfExists(migrationBuilder, "IX_Student_Group_Allocations_Student_Group_ID1", "Student_Group_Allocations");
            DropIndexIfExists(migrationBuilder, "IX_Session_Attendances_Attendance_Status_ID1", "Session_Attendances");
            DropIndexIfExists(migrationBuilder, "IX_Quizzes_Module_Code1", "Quizzes");
            DropIndexIfExists(migrationBuilder, "IX_Payments_Module_Code1", "Payments");
            DropIndexIfExists(migrationBuilder, "IX_Payments_StudentUser_ID", "Payments");
            DropIndexIfExists(migrationBuilder, "IX_Notifications_User_ID1", "Notifications");
            DropIndexIfExists(migrationBuilder, "IX_Module_Resources_Module_Code1", "Module_Resources");
            DropIndexIfExists(migrationBuilder, "IX_Bookings_StudentUser_ID", "Bookings");
            DropIndexIfExists(migrationBuilder, "IX_Assignments_Module_Code1", "Assignments");
            DropIndexIfExists(migrationBuilder, "IX_Admin_Profiles_User_ID1", "Admin_Profiles");

            DropColumnIfExists(migrationBuilder, "User_ID1", "Tutor_Profiles");
            DropColumnIfExists(migrationBuilder, "StudentUser_ID", "Student_Unenrollments");
            DropColumnIfExists(migrationBuilder, "Quiz_ID1", "Student_Quizzes");
            DropColumnIfExists(migrationBuilder, "User_ID1", "Student_Profiles");
            DropColumnIfExists(migrationBuilder, "Student_Group_ID1", "Student_Group_Allocations");
            DropColumnIfExists(migrationBuilder, "Attendance_Status_ID1", "Session_Attendances");
            DropColumnIfExists(migrationBuilder, "Module_Code1", "Quizzes");
            DropColumnIfExists(migrationBuilder, "Module_Code1", "Payments");
            DropColumnIfExists(migrationBuilder, "StudentUser_ID", "Payments");
            DropColumnIfExists(migrationBuilder, "User_ID1", "Notifications");
            DropColumnIfExists(migrationBuilder, "Module_Code1", "Module_Resources");
            DropColumnIfExists(migrationBuilder, "StudentUser_ID", "Bookings");
            DropColumnIfExists(migrationBuilder, "Module_Code1", "Assignments");
            DropColumnIfExists(migrationBuilder, "User_ID1", "Admin_Profiles");

            migrationBuilder.AlterColumn<string>(
                name: "Module_Code",
                table: "Quizzes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Module_Code",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Module_Code",
                table: "Module_Resources",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Module_Code",
                table: "Assignments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            DropIndexIfExists(migrationBuilder, "IX_Tutor_Reviews_Student_ID", "Tutor_Reviews");
            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Reviews_Student_ID",
                table: "Tutor_Reviews",
                column: "Student_ID");

            DropIndexIfExists(migrationBuilder, "IX_Tutor_Reviews_Tutor_ID", "Tutor_Reviews");
            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Reviews_Tutor_ID",
                table: "Tutor_Reviews",
                column: "Tutor_ID");

            DropIndexIfExists(migrationBuilder, "IX_Tutor_Profiles_User_ID", "Tutor_Profiles");
            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Profiles_User_ID",
                table: "Tutor_Profiles",
                column: "User_ID");

            DropIndexIfExists(migrationBuilder, "IX_Testimonials_Student_ID", "Testimonials");
            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_Student_ID",
                table: "Testimonials",
                column: "Student_ID");

            DropIndexIfExists(migrationBuilder, "IX_Testimonials_Testimonial_Category_ID", "Testimonials");
            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_Testimonial_Category_ID",
                table: "Testimonials",
                column: "Testimonial_Category_ID");

            DropIndexIfExists(migrationBuilder, "IX_Student_Unenrollments_Student_ID", "Student_Unenrollments");
            migrationBuilder.CreateIndex(
                name: "IX_Student_Unenrollments_Student_ID",
                table: "Student_Unenrollments",
                column: "Student_ID");

            DropIndexIfExists(migrationBuilder, "IX_Student_Quizzes_Quiz_ID", "Student_Quizzes");
            migrationBuilder.CreateIndex(
                name: "IX_Student_Quizzes_Quiz_ID",
                table: "Student_Quizzes",
                column: "Quiz_ID");

            DropIndexIfExists(migrationBuilder, "IX_Student_Quizzes_Student_ID", "Student_Quizzes");
            migrationBuilder.CreateIndex(
                name: "IX_Student_Quizzes_Student_ID",
                table: "Student_Quizzes",
                column: "Student_ID");

            DropIndexIfExists(migrationBuilder, "IX_Student_Quiz_Answers_Option_ID", "Student_Quiz_Answers");
            migrationBuilder.CreateIndex(
                name: "IX_Student_Quiz_Answers_Option_ID",
                table: "Student_Quiz_Answers",
                column: "Option_ID");

            DropIndexIfExists(migrationBuilder, "IX_Student_Quiz_Answers_Question_ID", "Student_Quiz_Answers");
            migrationBuilder.CreateIndex(
                name: "IX_Student_Quiz_Answers_Question_ID",
                table: "Student_Quiz_Answers",
                column: "Question_ID");

            DropIndexIfExists(migrationBuilder, "IX_Student_Quiz_Answers_Student_Quiz_ID", "Student_Quiz_Answers");
            migrationBuilder.CreateIndex(
                name: "IX_Student_Quiz_Answers_Student_Quiz_ID",
                table: "Student_Quiz_Answers",
                column: "Student_Quiz_ID");

            DropIndexIfExists(migrationBuilder, "IX_Student_Profiles_User_ID", "Student_Profiles");
            migrationBuilder.CreateIndex(
                name: "IX_Student_Profiles_User_ID",
                table: "Student_Profiles",
                column: "User_ID");

            DropIndexIfExists(migrationBuilder, "IX_Student_Group_Allocations_Student_Group_ID", "Student_Group_Allocations");
            migrationBuilder.CreateIndex(
                name: "IX_Student_Group_Allocations_Student_Group_ID",
                table: "Student_Group_Allocations",
                column: "Student_Group_ID");

            DropIndexIfExists(migrationBuilder, "IX_Student_Group_Allocations_Student_ID", "Student_Group_Allocations");
            migrationBuilder.CreateIndex(
                name: "IX_Student_Group_Allocations_Student_ID",
                table: "Student_Group_Allocations",
                column: "Student_ID");

            DropIndexIfExists(migrationBuilder, "IX_Session_Reviews_Student_ID", "Session_Reviews");
            migrationBuilder.CreateIndex(
                name: "IX_Session_Reviews_Student_ID",
                table: "Session_Reviews",
                column: "Student_ID");

            DropIndexIfExists(migrationBuilder, "IX_Session_Attendances_Attendance_Status_ID", "Session_Attendances");
            migrationBuilder.CreateIndex(
                name: "IX_Session_Attendances_Attendance_Status_ID",
                table: "Session_Attendances",
                column: "Attendance_Status_ID");

            DropIndexIfExists(migrationBuilder, "IX_Quizzes_Module_Code", "Quizzes");
            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_Module_Code",
                table: "Quizzes",
                column: "Module_Code");

            DropIndexIfExists(migrationBuilder, "IX_Quiz_Questions_Quiz_ID", "Quiz_Questions");
            migrationBuilder.CreateIndex(
                name: "IX_Quiz_Questions_Quiz_ID",
                table: "Quiz_Questions",
                column: "Quiz_ID");

            DropIndexIfExists(migrationBuilder, "IX_Quiz_Question_Options_Question_ID", "Quiz_Question_Options");
            migrationBuilder.CreateIndex(
                name: "IX_Quiz_Question_Options_Question_ID",
                table: "Quiz_Question_Options",
                column: "Question_ID");

            DropIndexIfExists(migrationBuilder, "IX_Payments_Module_Code", "Payments");
            migrationBuilder.CreateIndex(
                name: "IX_Payments_Module_Code",
                table: "Payments",
                column: "Module_Code");

            DropIndexIfExists(migrationBuilder, "IX_Payments_Student_ID", "Payments");
            migrationBuilder.CreateIndex(
                name: "IX_Payments_Student_ID",
                table: "Payments",
                column: "Student_ID");

            DropIndexIfExists(migrationBuilder, "IX_Notifications_User_ID", "Notifications");
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_ID",
                table: "Notifications",
                column: "User_ID");

            DropIndexIfExists(migrationBuilder, "IX_Module_Wishlists_Student_ID", "Module_Wishlists");
            migrationBuilder.CreateIndex(
                name: "IX_Module_Wishlists_Student_ID",
                table: "Module_Wishlists",
                column: "Student_ID");

            DropIndexIfExists(migrationBuilder, "IX_Module_Resources_Module_Code", "Module_Resources");
            migrationBuilder.CreateIndex(
                name: "IX_Module_Resources_Module_Code",
                table: "Module_Resources",
                column: "Module_Code");

            DropIndexIfExists(migrationBuilder, "IX_FAQs_FAQ_Category_ID", "FAQs");
            migrationBuilder.CreateIndex(
                name: "IX_FAQs_FAQ_Category_ID",
                table: "FAQs",
                column: "FAQ_Category_ID");

            DropIndexIfExists(migrationBuilder, "IX_Bookings_Student_ID", "Bookings");
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Student_ID",
                table: "Bookings",
                column: "Student_ID");

            DropIndexIfExists(migrationBuilder, "IX_Assignments_Module_Code", "Assignments");
            migrationBuilder.CreateIndex(
                name: "IX_Assignments_Module_Code",
                table: "Assignments",
                column: "Module_Code");

            DropIndexIfExists(migrationBuilder, "IX_Admin_Profiles_User_ID", "Admin_Profiles");
            migrationBuilder.CreateIndex(
                name: "IX_Admin_Profiles_User_ID",
                table: "Admin_Profiles",
                column: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Admin_Profiles_Users_User_ID", "Admin_Profiles");
            migrationBuilder.AddForeignKey(
                name: "FK_Admin_Profiles_Users_User_ID",
                table: "Admin_Profiles",
                column: "User_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Assignment_Submissions_Users_Student_ID", "Assignment_Submissions");
            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Submissions_Users_Student_ID",
                table: "Assignment_Submissions",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Assignments_Modules_Module_Code", "Assignments");
            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Modules_Module_Code",
                table: "Assignments",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Booking_Slots_Users_Tutor_ID", "Booking_Slots");
            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Slots_Users_Tutor_ID",
                table: "Booking_Slots",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Restrict);

            DropForeignKeyIfExists(migrationBuilder, "FK_Bookings_Users_Student_ID", "Bookings");
            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_Student_ID",
                table: "Bookings",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_FAQs_FAQ_Categories_FAQ_Category_ID", "FAQs");
            migrationBuilder.AddForeignKey(
                name: "FK_FAQs_FAQ_Categories_FAQ_Category_ID",
                table: "FAQs",
                column: "FAQ_Category_ID",
                principalTable: "FAQ_Categories",
                principalColumn: "FAQ_Category_ID",
                onDelete: ReferentialAction.Restrict);

            DropForeignKeyIfExists(migrationBuilder, "FK_Log_Hours_Users_Tutor_ID", "Log_Hours");
            migrationBuilder.AddForeignKey(
                name: "FK_Log_Hours_Users_Tutor_ID",
                table: "Log_Hours",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Restrict);

            DropForeignKeyIfExists(migrationBuilder, "FK_Module_Resources_Modules_Module_Code", "Module_Resources");
            migrationBuilder.AddForeignKey(
                name: "FK_Module_Resources_Modules_Module_Code",
                table: "Module_Resources",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Module_Wishlists_Users_Student_ID", "Module_Wishlists");
            migrationBuilder.AddForeignKey(
                name: "FK_Module_Wishlists_Users_Student_ID",
                table: "Module_Wishlists",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Notifications_Users_User_ID", "Notifications");
            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_User_ID",
                table: "Notifications",
                column: "User_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Payments_Modules_Module_Code", "Payments");
            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Modules_Module_Code",
                table: "Payments",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Restrict);

            DropForeignKeyIfExists(migrationBuilder, "FK_Payments_Users_Student_ID", "Payments");
            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_Student_ID",
                table: "Payments",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Restrict);

            DropForeignKeyIfExists(migrationBuilder, "FK_Quiz_Question_Options_Quiz_Questions_Question_ID", "Quiz_Question_Options");
            migrationBuilder.AddForeignKey(
                name: "FK_Quiz_Question_Options_Quiz_Questions_Question_ID",
                table: "Quiz_Question_Options",
                column: "Question_ID",
                principalTable: "Quiz_Questions",
                principalColumn: "Question_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Quiz_Questions_Quizzes_Quiz_ID", "Quiz_Questions");
            migrationBuilder.AddForeignKey(
                name: "FK_Quiz_Questions_Quizzes_Quiz_ID",
                table: "Quiz_Questions",
                column: "Quiz_ID",
                principalTable: "Quizzes",
                principalColumn: "Quiz_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Quizzes_Modules_Module_Code", "Quizzes");
            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Modules_Module_Code",
                table: "Quizzes",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Session_Attendances_Attendance_Statuses_Attendance_Status_ID", "Session_Attendances");
            migrationBuilder.AddForeignKey(
                name: "FK_Session_Attendances_Attendance_Statuses_Attendance_Status_ID",
                table: "Session_Attendances",
                column: "Attendance_Status_ID",
                principalTable: "Attendance_Statuses",
                principalColumn: "Attendance_Status_ID",
                onDelete: ReferentialAction.Restrict);

            DropForeignKeyIfExists(migrationBuilder, "FK_Session_Reviews_Users_Student_ID", "Session_Reviews");
            migrationBuilder.AddForeignKey(
                name: "FK_Session_Reviews_Users_Student_ID",
                table: "Session_Reviews",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Group_Allocations_Student_Groups_Student_Group_ID", "Student_Group_Allocations");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Group_Allocations_Student_Groups_Student_Group_ID",
                table: "Student_Group_Allocations",
                column: "Student_Group_ID",
                principalTable: "Student_Groups",
                principalColumn: "Student_Group_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Group_Allocations_Users_Student_ID", "Student_Group_Allocations");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Group_Allocations_Users_Student_ID",
                table: "Student_Group_Allocations",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Modules_Users_Student_ID", "Student_Modules");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Modules_Users_Student_ID",
                table: "Student_Modules",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Profiles_Users_User_ID", "Student_Profiles");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Profiles_Users_User_ID",
                table: "Student_Profiles",
                column: "User_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Quiz_Answers_Quiz_Question_Options_Option_ID", "Student_Quiz_Answers");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quiz_Answers_Quiz_Question_Options_Option_ID",
                table: "Student_Quiz_Answers",
                column: "Option_ID",
                principalTable: "Quiz_Question_Options",
                principalColumn: "Option_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Quiz_Answers_Quiz_Questions_Question_ID", "Student_Quiz_Answers");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quiz_Answers_Quiz_Questions_Question_ID",
                table: "Student_Quiz_Answers",
                column: "Question_ID",
                principalTable: "Quiz_Questions",
                principalColumn: "Question_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Quiz_Answers_Student_Quizzes_Student_Quiz_ID", "Student_Quiz_Answers");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quiz_Answers_Student_Quizzes_Student_Quiz_ID",
                table: "Student_Quiz_Answers",
                column: "Student_Quiz_ID",
                principalTable: "Student_Quizzes",
                principalColumn: "Student_Quiz_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Quizzes_Quizzes_Quiz_ID", "Student_Quizzes");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quizzes_Quizzes_Quiz_ID",
                table: "Student_Quizzes",
                column: "Quiz_ID",
                principalTable: "Quizzes",
                principalColumn: "Quiz_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Quizzes_Users_Student_ID", "Student_Quizzes");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quizzes_Users_Student_ID",
                table: "Student_Quizzes",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Unenrollments_Modules_Module_Code", "Student_Unenrollments");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Unenrollments_Modules_Module_Code",
                table: "Student_Unenrollments",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Restrict);

            DropForeignKeyIfExists(migrationBuilder, "FK_Student_Unenrollments_Users_Student_ID", "Student_Unenrollments");
            migrationBuilder.AddForeignKey(
                name: "FK_Student_Unenrollments_Users_Student_ID",
                table: "Student_Unenrollments",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Testimonials_Testimonial_Categories_Testimonial_Category_ID", "Testimonials");
            migrationBuilder.AddForeignKey(
                name: "FK_Testimonials_Testimonial_Categories_Testimonial_Category_ID",
                table: "Testimonials",
                column: "Testimonial_Category_ID",
                principalTable: "Testimonial_Categories",
                principalColumn: "Testimonial_Category_ID",
                onDelete: ReferentialAction.Restrict);

            DropForeignKeyIfExists(migrationBuilder, "FK_Testimonials_Users_Student_ID", "Testimonials");
            migrationBuilder.AddForeignKey(
                name: "FK_Testimonials_Users_Student_ID",
                table: "Testimonials",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Tutor_Modules_Users_Tutor_ID", "Tutor_Modules");
            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Modules_Users_Tutor_ID",
                table: "Tutor_Modules",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Tutor_Profiles_Users_User_ID", "Tutor_Profiles");
            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Profiles_Users_User_ID",
                table: "Tutor_Profiles",
                column: "User_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            DropForeignKeyIfExists(migrationBuilder, "FK_Tutor_Reviews_Users_Student_ID", "Tutor_Reviews");
            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Reviews_Users_Student_ID",
                table: "Tutor_Reviews",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Tutor_Reviews_Users_Tutor_ID", "Tutor_Reviews");
            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Reviews_Users_Tutor_ID",
                table: "Tutor_Reviews",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            DropForeignKeyIfExists(migrationBuilder, "FK_Users_User_Roles_User_Role_ID", "Users");
            migrationBuilder.AddForeignKey(
                name: "FK_Users_User_Roles_User_Role_ID",
                table: "Users",
                column: "User_Role_ID",
                principalTable: "User_Roles",
                principalColumn: "User_Role_ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Admin_Profiles_Users_User_ID",
                table: "Admin_Profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_Submissions_Users_Student_ID",
                table: "Assignment_Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Modules_Module_Code",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Booking_Slots_Users_Tutor_ID",
                table: "Booking_Slots");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_Student_ID",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FAQs_FAQ_Categories_FAQ_Category_ID",
                table: "FAQs");

            migrationBuilder.DropForeignKey(
                name: "FK_Log_Hours_Users_Tutor_ID",
                table: "Log_Hours");

            migrationBuilder.DropForeignKey(
                name: "FK_Module_Resources_Modules_Module_Code",
                table: "Module_Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Module_Wishlists_Users_Student_ID",
                table: "Module_Wishlists");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_User_ID",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Modules_Module_Code",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_Student_ID",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Quiz_Question_Options_Quiz_Questions_Question_ID",
                table: "Quiz_Question_Options");

            migrationBuilder.DropForeignKey(
                name: "FK_Quiz_Questions_Quizzes_Quiz_ID",
                table: "Quiz_Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Modules_Module_Code",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Session_Attendances_Attendance_Statuses_Attendance_Status_ID",
                table: "Session_Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Session_Reviews_Users_Student_ID",
                table: "Session_Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Group_Allocations_Student_Groups_Student_Group_ID",
                table: "Student_Group_Allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Group_Allocations_Users_Student_ID",
                table: "Student_Group_Allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Modules_Users_Student_ID",
                table: "Student_Modules");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Profiles_Users_User_ID",
                table: "Student_Profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Quiz_Answers_Quiz_Question_Options_Option_ID",
                table: "Student_Quiz_Answers");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Quiz_Answers_Quiz_Questions_Question_ID",
                table: "Student_Quiz_Answers");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Quiz_Answers_Student_Quizzes_Student_Quiz_ID",
                table: "Student_Quiz_Answers");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Quizzes_Quizzes_Quiz_ID",
                table: "Student_Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Quizzes_Users_Student_ID",
                table: "Student_Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Unenrollments_Modules_Module_Code",
                table: "Student_Unenrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Unenrollments_Users_Student_ID",
                table: "Student_Unenrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Testimonials_Testimonial_Categories_Testimonial_Category_ID",
                table: "Testimonials");

            migrationBuilder.DropForeignKey(
                name: "FK_Testimonials_Users_Student_ID",
                table: "Testimonials");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutor_Modules_Users_Tutor_ID",
                table: "Tutor_Modules");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutor_Profiles_Users_User_ID",
                table: "Tutor_Profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutor_Reviews_Users_Student_ID",
                table: "Tutor_Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutor_Reviews_Users_Tutor_ID",
                table: "Tutor_Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_User_Roles_User_Role_ID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tutor_Reviews_Student_ID",
                table: "Tutor_Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Tutor_Reviews_Tutor_ID",
                table: "Tutor_Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Tutor_Profiles_User_ID",
                table: "Tutor_Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Testimonials_Student_ID",
                table: "Testimonials");

            migrationBuilder.DropIndex(
                name: "IX_Testimonials_Testimonial_Category_ID",
                table: "Testimonials");

            migrationBuilder.DropIndex(
                name: "IX_Student_Unenrollments_Student_ID",
                table: "Student_Unenrollments");

            migrationBuilder.DropIndex(
                name: "IX_Student_Quizzes_Quiz_ID",
                table: "Student_Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Student_Quizzes_Student_ID",
                table: "Student_Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Student_Quiz_Answers_Option_ID",
                table: "Student_Quiz_Answers");

            migrationBuilder.DropIndex(
                name: "IX_Student_Quiz_Answers_Question_ID",
                table: "Student_Quiz_Answers");

            migrationBuilder.DropIndex(
                name: "IX_Student_Quiz_Answers_Student_Quiz_ID",
                table: "Student_Quiz_Answers");

            migrationBuilder.DropIndex(
                name: "IX_Student_Profiles_User_ID",
                table: "Student_Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Student_Group_Allocations_Student_Group_ID",
                table: "Student_Group_Allocations");

            migrationBuilder.DropIndex(
                name: "IX_Student_Group_Allocations_Student_ID",
                table: "Student_Group_Allocations");

            migrationBuilder.DropIndex(
                name: "IX_Session_Reviews_Student_ID",
                table: "Session_Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Session_Attendances_Attendance_Status_ID",
                table: "Session_Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_Module_Code",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quiz_Questions_Quiz_ID",
                table: "Quiz_Questions");

            migrationBuilder.DropIndex(
                name: "IX_Quiz_Question_Options_Question_ID",
                table: "Quiz_Question_Options");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Module_Code",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Student_ID",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_User_ID",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Module_Wishlists_Student_ID",
                table: "Module_Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_Module_Resources_Module_Code",
                table: "Module_Resources");

            migrationBuilder.DropIndex(
                name: "IX_FAQs_FAQ_Category_ID",
                table: "FAQs");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Student_ID",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_Module_Code",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Admin_Profiles_User_ID",
                table: "Admin_Profiles");

            migrationBuilder.AddColumn<int>(
                name: "User_ID1",
                table: "Tutor_Profiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentUser_ID",
                table: "Student_Unenrollments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quiz_ID1",
                table: "Student_Quizzes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "User_ID1",
                table: "Student_Profiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Student_Group_ID1",
                table: "Student_Group_Allocations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attendance_Status_ID1",
                table: "Session_Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Module_Code",
                table: "Quizzes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "Module_Code1",
                table: "Quizzes",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Module_Code",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "Module_Code1",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentUser_ID",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "User_ID1",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Module_Code",
                table: "Module_Resources",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "Module_Code1",
                table: "Module_Resources",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentUser_ID",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Module_Code",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "Module_Code1",
                table: "Assignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "User_ID1",
                table: "Admin_Profiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Profiles_User_ID1",
                table: "Tutor_Profiles",
                column: "User_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Unenrollments_StudentUser_ID",
                table: "Student_Unenrollments",
                column: "StudentUser_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Quizzes_Quiz_ID1",
                table: "Student_Quizzes",
                column: "Quiz_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Profiles_User_ID1",
                table: "Student_Profiles",
                column: "User_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Group_Allocations_Student_Group_ID1",
                table: "Student_Group_Allocations",
                column: "Student_Group_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Session_Attendances_Attendance_Status_ID1",
                table: "Session_Attendances",
                column: "Attendance_Status_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_Module_Code1",
                table: "Quizzes",
                column: "Module_Code1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Module_Code1",
                table: "Payments",
                column: "Module_Code1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_StudentUser_ID",
                table: "Payments",
                column: "StudentUser_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_ID1",
                table: "Notifications",
                column: "User_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Module_Resources_Module_Code1",
                table: "Module_Resources",
                column: "Module_Code1");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StudentUser_ID",
                table: "Bookings",
                column: "StudentUser_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_Module_Code1",
                table: "Assignments",
                column: "Module_Code1");

            migrationBuilder.CreateIndex(
                name: "IX_Admin_Profiles_User_ID1",
                table: "Admin_Profiles",
                column: "User_ID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Admin_Profiles_Users_User_ID1",
                table: "Admin_Profiles",
                column: "User_ID1",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Submissions_Users_Student_ID",
                table: "Assignment_Submissions",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Modules_Module_Code1",
                table: "Assignments",
                column: "Module_Code1",
                principalTable: "Modules",
                principalColumn: "Module_Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Slots_Users_Tutor_ID",
                table: "Booking_Slots",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_StudentUser_ID",
                table: "Bookings",
                column: "StudentUser_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Log_Hours_Users_Tutor_ID",
                table: "Log_Hours",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Module_Resources_Modules_Module_Code1",
                table: "Module_Resources",
                column: "Module_Code1",
                principalTable: "Modules",
                principalColumn: "Module_Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_User_ID1",
                table: "Notifications",
                column: "User_ID1",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Modules_Module_Code1",
                table: "Payments",
                column: "Module_Code1",
                principalTable: "Modules",
                principalColumn: "Module_Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_StudentUser_ID",
                table: "Payments",
                column: "StudentUser_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Modules_Module_Code1",
                table: "Quizzes",
                column: "Module_Code1",
                principalTable: "Modules",
                principalColumn: "Module_Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Session_Attendances_Attendance_Statuses_Attendance_Status_ID1",
                table: "Session_Attendances",
                column: "Attendance_Status_ID1",
                principalTable: "Attendance_Statuses",
                principalColumn: "Attendance_Status_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Group_Allocations_Student_Groups_Student_Group_ID1",
                table: "Student_Group_Allocations",
                column: "Student_Group_ID1",
                principalTable: "Student_Groups",
                principalColumn: "Student_Group_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Modules_Users_Student_ID",
                table: "Student_Modules",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Profiles_Users_User_ID1",
                table: "Student_Profiles",
                column: "User_ID1",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quizzes_Quizzes_Quiz_ID1",
                table: "Student_Quizzes",
                column: "Quiz_ID1",
                principalTable: "Quizzes",
                principalColumn: "Quiz_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Unenrollments_Modules_Module_Code",
                table: "Student_Unenrollments",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Unenrollments_Users_StudentUser_ID",
                table: "Student_Unenrollments",
                column: "StudentUser_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Modules_Users_Tutor_ID",
                table: "Tutor_Modules",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Profiles_Users_User_ID1",
                table: "Tutor_Profiles",
                column: "User_ID1",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_User_Roles_User_Role_ID",
                table: "Users",
                column: "User_Role_ID",
                principalTable: "User_Roles",
                principalColumn: "User_Role_ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
