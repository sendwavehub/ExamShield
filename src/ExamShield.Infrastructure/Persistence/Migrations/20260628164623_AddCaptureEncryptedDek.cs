using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamShield.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptureEncryptedDek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedDek",
                table: "Captures",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedDek",
                table: "Captures");
        }
    }
}
