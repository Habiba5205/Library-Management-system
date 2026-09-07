using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lib_System.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoanDays",
                table: "Borrowings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationExpiresAtUtc",
                table: "Borrowings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoanDays",
                table: "Borrowings");

            migrationBuilder.DropColumn(
                name: "ReservationExpiresAtUtc",
                table: "Borrowings");
        }
    }
}
