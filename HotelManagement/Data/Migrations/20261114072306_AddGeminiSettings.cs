using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGeminiSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeminiApiKeyProtected",
                table: "CaiDatHeThongs",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GeminiModel",
                table: "CaiDatHeThongs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "GeminiTimeoutSeconds",
                table: "CaiDatHeThongs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeminiApiKeyProtected",
                table: "CaiDatHeThongs");

            migrationBuilder.DropColumn(
                name: "GeminiModel",
                table: "CaiDatHeThongs");

            migrationBuilder.DropColumn(
                name: "GeminiTimeoutSeconds",
                table: "CaiDatHeThongs");
        }
    }
}
