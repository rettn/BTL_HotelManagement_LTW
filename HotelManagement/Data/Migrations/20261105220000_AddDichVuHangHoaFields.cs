using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using HotelManagement.Data;

#nullable disable

namespace HotelManagement.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261105220000_AddDichVuHangHoaFields")]
    public partial class AddDichVuHangHoaFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "MaHang", table: "DichVus", type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "").Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "LoaiHang", table: "DichVus", type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "DichVu").Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "NhomHang", table: "DichVus", type: "varchar(80)", maxLength: 80, nullable: false, defaultValue: "Dịch vụ").Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "DonViTinh", table: "DichVus", type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "Lần").Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<decimal>(name: "GiaVon", table: "DichVus", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<int>(name: "TonKho", table: "DichVus", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(name: "AnhUrl", table: "DichVus", type: "varchar(500)", maxLength: 500, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<bool>(name: "YeuThich", table: "DichVus", type: "tinyint(1)", nullable: false, defaultValue: false);
            migrationBuilder.Sql("UPDATE DichVus SET MaHang = CONCAT('DV', LPAD(Id, 6, '0')) WHERE MaHang = '';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MaHang", table: "DichVus");
            migrationBuilder.DropColumn(name: "LoaiHang", table: "DichVus");
            migrationBuilder.DropColumn(name: "NhomHang", table: "DichVus");
            migrationBuilder.DropColumn(name: "DonViTinh", table: "DichVus");
            migrationBuilder.DropColumn(name: "GiaVon", table: "DichVus");
            migrationBuilder.DropColumn(name: "TonKho", table: "DichVus");
            migrationBuilder.DropColumn(name: "AnhUrl", table: "DichVus");
            migrationBuilder.DropColumn(name: "YeuThich", table: "DichVus");
        }
    }
}
