using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFullReferentialIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Admin_Profiles_Users_User_ID1",
                table: "Admin_Profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_Submissions_Users_Student_ID",
                table: "Assignment_Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Modules_Module_Code1",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Booking_Slots_Users_Tutor_ID",
                table: "Booking_Slots");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_StudentUser_ID",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Log_Hours_Users_Tutor_ID",
                table: "Log_Hours");

            migrationBuilder.DropForeignKey(
                name: "FK_Module_Resources_Modules_Module_Code1",
                table: "Module_Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_User_ID1",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Modules_Module_Code1",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_StudentUser_ID",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Modules_Module_Code1",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Session_Attendances_Attendance_Statuses_Attendance_Status_ID1",
                table: "Session_Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Group_Allocations_Student_Groups_Student_Group_ID1",
                table: "Student_Group_Allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Modules_Users_Student_ID",
                table: "Student_Modules");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Profiles_Users_User_ID1",
                table: "Student_Profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Quizzes_Quizzes_Quiz_ID1",
                table: "Student_Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Unenrollments_Modules_Module_Code",
                table: "Student_Unenrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Unenrollments_Users_StudentUser_ID",
                table: "Student_Unenrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutor_Modules_Users_Tutor_ID",
                table: "Tutor_Modules");

            migrationBuilder.DropForeignKey(
                name: "FK_Tutor_Profiles_Users_User_ID1",
                table: "Tutor_Profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_User_Roles_User_Role_ID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tutor_Profiles_User_ID1",
                table: "Tutor_Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Student_Unenrollments_StudentUser_ID",
                table: "Student_Unenrollments");

            migrationBuilder.DropIndex(
                name: "IX_Student_Quizzes_Quiz_ID1",
                table: "Student_Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Student_Profiles_User_ID1",
                table: "Student_Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Student_Group_Allocations_Student_Group_ID1",
                table: "Student_Group_Allocations");

            migrationBuilder.DropIndex(
                name: "IX_Session_Attendances_Attendance_Status_ID1",
                table: "Session_Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_Module_Code1",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Module_Code1",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_StudentUser_ID",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_User_ID1",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Module_Resources_Module_Code1",
                table: "Module_Resources");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_StudentUser_ID",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_Module_Code1",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Admin_Profiles_User_ID1",
                table: "Admin_Profiles");

            migrationBuilder.DropColumn(
                name: "User_ID1",
                table: "Tutor_Profiles");

            migrationBuilder.DropColumn(
                name: "StudentUser_ID",
                table: "Student_Unenrollments");

            migrationBuilder.DropColumn(
                name: "Quiz_ID1",
                table: "Student_Quizzes");

            migrationBuilder.DropColumn(
                name: "User_ID1",
                table: "Student_Profiles");

            migrationBuilder.DropColumn(
                name: "Student_Group_ID1",
                table: "Student_Group_Allocations");

            migrationBuilder.DropColumn(
                name: "Attendance_Status_ID1",
                table: "Session_Attendances");

            migrationBuilder.DropColumn(
                name: "Module_Code1",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "Module_Code1",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "StudentUser_ID",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "User_ID1",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Module_Code1",
                table: "Module_Resources");

            migrationBuilder.DropColumn(
                name: "StudentUser_ID",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Module_Code1",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "User_ID1",
                table: "Admin_Profiles");

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

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Reviews_Student_ID",
                table: "Tutor_Reviews",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Reviews_Tutor_ID",
                table: "Tutor_Reviews",
                column: "Tutor_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Profiles_User_ID",
                table: "Tutor_Profiles",
                column: "User_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_Student_ID",
                table: "Testimonials",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_Testimonial_Category_ID",
                table: "Testimonials",
                column: "Testimonial_Category_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Unenrollments_Student_ID",
                table: "Student_Unenrollments",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Quizzes_Quiz_ID",
                table: "Student_Quizzes",
                column: "Quiz_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Quizzes_Student_ID",
                table: "Student_Quizzes",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Quiz_Answers_Option_ID",
                table: "Student_Quiz_Answers",
                column: "Option_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Quiz_Answers_Question_ID",
                table: "Student_Quiz_Answers",
                column: "Question_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Quiz_Answers_Student_Quiz_ID",
                table: "Student_Quiz_Answers",
                column: "Student_Quiz_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Profiles_User_ID",
                table: "Student_Profiles",
                column: "User_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Group_Allocations_Student_Group_ID",
                table: "Student_Group_Allocations",
                column: "Student_Group_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Group_Allocations_Student_ID",
                table: "Student_Group_Allocations",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Session_Reviews_Student_ID",
                table: "Session_Reviews",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Session_Attendances_Attendance_Status_ID",
                table: "Session_Attendances",
                column: "Attendance_Status_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_Module_Code",
                table: "Quizzes",
                column: "Module_Code");

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_Questions_Quiz_ID",
                table: "Quiz_Questions",
                column: "Quiz_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_Question_Options_Question_ID",
                table: "Quiz_Question_Options",
                column: "Question_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Module_Code",
                table: "Payments",
                column: "Module_Code");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Student_ID",
                table: "Payments",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_ID",
                table: "Notifications",
                column: "User_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Module_Wishlists_Student_ID",
                table: "Module_Wishlists",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Module_Resources_Module_Code",
                table: "Module_Resources",
                column: "Module_Code");

            migrationBuilder.CreateIndex(
                name: "IX_FAQs_FAQ_Category_ID",
                table: "FAQs",
                column: "FAQ_Category_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Student_ID",
                table: "Bookings",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_Module_Code",
                table: "Assignments",
                column: "Module_Code");

            migrationBuilder.CreateIndex(
                name: "IX_Admin_Profiles_User_ID",
                table: "Admin_Profiles",
                column: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Admin_Profiles_Users_User_ID",
                table: "Admin_Profiles",
                column: "User_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Submissions_Users_Student_ID",
                table: "Assignment_Submissions",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Modules_Module_Code",
                table: "Assignments",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Slots_Users_Tutor_ID",
                table: "Booking_Slots",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_Student_ID",
                table: "Bookings",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_FAQs_FAQ_Categories_FAQ_Category_ID",
                table: "FAQs",
                column: "FAQ_Category_ID",
                principalTable: "FAQ_Categories",
                principalColumn: "FAQ_Category_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Log_Hours_Users_Tutor_ID",
                table: "Log_Hours",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Module_Resources_Modules_Module_Code",
                table: "Module_Resources",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Module_Wishlists_Users_Student_ID",
                table: "Module_Wishlists",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_User_ID",
                table: "Notifications",
                column: "User_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Modules_Module_Code",
                table: "Payments",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_Student_ID",
                table: "Payments",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quiz_Question_Options_Quiz_Questions_Question_ID",
                table: "Quiz_Question_Options",
                column: "Question_ID",
                principalTable: "Quiz_Questions",
                principalColumn: "Question_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quiz_Questions_Quizzes_Quiz_ID",
                table: "Quiz_Questions",
                column: "Quiz_ID",
                principalTable: "Quizzes",
                principalColumn: "Quiz_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Modules_Module_Code",
                table: "Quizzes",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Session_Attendances_Attendance_Statuses_Attendance_Status_ID",
                table: "Session_Attendances",
                column: "Attendance_Status_ID",
                principalTable: "Attendance_Statuses",
                principalColumn: "Attendance_Status_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Session_Reviews_Users_Student_ID",
                table: "Session_Reviews",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Group_Allocations_Student_Groups_Student_Group_ID",
                table: "Student_Group_Allocations",
                column: "Student_Group_ID",
                principalTable: "Student_Groups",
                principalColumn: "Student_Group_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Group_Allocations_Users_Student_ID",
                table: "Student_Group_Allocations",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Modules_Users_Student_ID",
                table: "Student_Modules",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Profiles_Users_User_ID",
                table: "Student_Profiles",
                column: "User_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quiz_Answers_Quiz_Question_Options_Option_ID",
                table: "Student_Quiz_Answers",
                column: "Option_ID",
                principalTable: "Quiz_Question_Options",
                principalColumn: "Option_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quiz_Answers_Quiz_Questions_Question_ID",
                table: "Student_Quiz_Answers",
                column: "Question_ID",
                principalTable: "Quiz_Questions",
                principalColumn: "Question_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quiz_Answers_Student_Quizzes_Student_Quiz_ID",
                table: "Student_Quiz_Answers",
                column: "Student_Quiz_ID",
                principalTable: "Student_Quizzes",
                principalColumn: "Student_Quiz_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quizzes_Quizzes_Quiz_ID",
                table: "Student_Quizzes",
                column: "Quiz_ID",
                principalTable: "Quizzes",
                principalColumn: "Quiz_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Quizzes_Users_Student_ID",
                table: "Student_Quizzes",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Unenrollments_Modules_Module_Code",
                table: "Student_Unenrollments",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Unenrollments_Users_Student_ID",
                table: "Student_Unenrollments",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Testimonials_Testimonial_Categories_Testimonial_Category_ID",
                table: "Testimonials",
                column: "Testimonial_Category_ID",
                principalTable: "Testimonial_Categories",
                principalColumn: "Testimonial_Category_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Testimonials_Users_Student_ID",
                table: "Testimonials",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Modules_Users_Tutor_ID",
                table: "Tutor_Modules",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Profiles_Users_User_ID",
                table: "Tutor_Profiles",
                column: "User_ID",
                principalTable: "Users",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Reviews_Users_Student_ID",
                table: "Tutor_Reviews",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Tutor_Reviews_Users_Tutor_ID",
                table: "Tutor_Reviews",
                column: "Tutor_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

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
