using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamShield.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScorePublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Scores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PublishedAt",
                table: "Scores",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Scores");
        }
    }
}
