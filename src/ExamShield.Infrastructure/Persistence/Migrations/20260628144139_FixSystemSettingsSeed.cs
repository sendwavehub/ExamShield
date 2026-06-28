using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamShield.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixSystemSettingsSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: 638396640000000000L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: 639182260182377950L);
        }
    }
}
