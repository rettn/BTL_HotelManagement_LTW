using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class SuaBangDichVu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietDichVu_DichVu_MaDichVu",
                table: "ChiTietDichVu");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DichVu",
                table: "DichVu");

            migrationBuilder.DropColumn(
                name: "DonGia",
                table: "DichVu");

            migrationBuilder.DropColumn(
                name: "DonViTinh",
                table: "DichVu");

            migrationBuilder.RenameTable(
                name: "DichVu",
                newName: "DichVus");

            migrationBuilder.RenameColumn(
                name: "MaDichVu",
                table: "DichVus",
                newName: "Id");

            migrationBuilder.UpdateData(
                table: "DichVus",
                keyColumn: "MoTa",
                keyValue: null,
                column: "MoTa",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "MoTa",
                table: "DichVus",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Gia",
                table: "DichVus",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DichVus",
                table: "DichVus",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietDichVu_DichVus_MaDichVu",
                table: "ChiTietDichVu",
                column: "MaDichVu",
                principalTable: "DichVus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietDichVu_DichVus_MaDichVu",
                table: "ChiTietDichVu");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DichVus",
                table: "DichVus");

            migrationBuilder.DropColumn(
                name: "Gia",
                table: "DichVus");

            migrationBuilder.RenameTable(
                name: "DichVus",
                newName: "DichVu");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "DichVu",
                newName: "MaDichVu");

            migrationBuilder.AlterColumn<string>(
                name: "MoTa",
                table: "DichVu",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DonGia",
                table: "DichVu",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DonViTinh",
                table: "DichVu",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DichVu",
                table: "DichVu",
                column: "MaDichVu");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietDichVu_DichVu_MaDichVu",
                table: "ChiTietDichVu",
                column: "MaDichVu",
                principalTable: "DichVu",
                principalColumn: "MaDichVu",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
