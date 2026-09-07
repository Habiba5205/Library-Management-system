using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lib_System.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomaticOverdueFines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fines_BorrowingId",
                table: "Fines");

            migrationBuilder.AddColumn<bool>(
                name: "IsAutomatic",
                table: "Fines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Fines_BorrowingId",
                table: "Fines",
                column: "BorrowingId",
                unique: true,
                filter: "[IsAutomatic] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fines_BorrowingId",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "IsAutomatic",
                table: "Fines");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_BorrowingId",
                table: "Fines",
                column: "BorrowingId");
        }
    }
}
