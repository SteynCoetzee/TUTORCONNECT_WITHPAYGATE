using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class ReferentialIntegrityAndValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Session_Reviews_Session_ID",
                table: "Session_Reviews",
                column: "Session_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Session_Attendances_Session_ID",
                table: "Session_Attendances",
                column: "Session_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Session_Attendances_Booking_Slots_Session_ID",
                table: "Session_Attendances",
                column: "Session_ID",
                principalTable: "Booking_Slots",
                principalColumn: "Booking_Slot_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Session_Reviews_Booking_Slots_Session_ID",
                table: "Session_Reviews",
                column: "Session_ID",
                principalTable: "Booking_Slots",
                principalColumn: "Booking_Slot_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Session_Attendances_Booking_Slots_Session_ID",
                table: "Session_Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Session_Reviews_Booking_Slots_Session_ID",
                table: "Session_Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Session_Reviews_Session_ID",
                table: "Session_Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Session_Attendances_Session_ID",
                table: "Session_Attendances");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
