using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lib_System.Migrations
{
    /// <inheritdoc />
    public partial class AddFinePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                table: "Fines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentAmount",
                table: "Fines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentAttemptId",
                table: "Fines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Fines",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Fines",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "PaymentAmount",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "PaymentAttemptId",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Fines");
        }
    }
}
