using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class CleanupRemainingFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Module_Code",
                table: "Booking_Slots",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Session_Attendances_Student_ID",
                table: "Session_Attendances",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Log_Hours_ApprovedBy_Admin_ID",
                table: "Log_Hours",
                column: "ApprovedBy_Admin_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_Slots_Module_Code",
                table: "Booking_Slots",
                column: "Module_Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Slots_Modules_Module_Code",
                table: "Booking_Slots",
                column: "Module_Code",
                principalTable: "Modules",
                principalColumn: "Module_Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Log_Hours_Users_ApprovedBy_Admin_ID",
                table: "Log_Hours",
                column: "ApprovedBy_Admin_ID",
                principalTable: "Users",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Session_Attendances_Users_Student_ID",
                table: "Session_Attendances",
                column: "Student_ID",
                principalTable: "Users",
                principalColumn: "User_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Booking_Slots_Modules_Module_Code",
                table: "Booking_Slots");

            migrationBuilder.DropForeignKey(
                name: "FK_Log_Hours_Users_ApprovedBy_Admin_ID",
                table: "Log_Hours");

            migrationBuilder.DropForeignKey(
                name: "FK_Session_Attendances_Users_Student_ID",
                table: "Session_Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Session_Attendances_Student_ID",
                table: "Session_Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Log_Hours_ApprovedBy_Admin_ID",
                table: "Log_Hours");

            migrationBuilder.DropIndex(
                name: "IX_Booking_Slots_Module_Code",
                table: "Booking_Slots");

            migrationBuilder.AlterColumn<string>(
                name: "Module_Code",
                table: "Booking_Slots",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

        }
    }
}
