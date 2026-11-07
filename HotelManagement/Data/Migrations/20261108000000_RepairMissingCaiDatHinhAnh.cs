using HotelManagement.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261108000000_RepairMissingCaiDatHinhAnh")]
    public partial class RepairMissingCaiDatHinhAnh : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET @hotel_hinhanh_sql = (SELECT IF(COUNT(*) = 0, 'ALTER TABLE `CaiDatHeThongs` ADD COLUMN `HinhAnh` varchar(500) CHARACTER SET utf8mb4 NULL', 'SELECT 1') FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CaiDatHeThongs' AND COLUMN_NAME = 'HinhAnh');");
            migrationBuilder.Sql("PREPARE hotel_hinhanh_stmt FROM @hotel_hinhanh_sql;");
            migrationBuilder.Sql("EXECUTE hotel_hinhanh_stmt;");
            migrationBuilder.Sql("DEALLOCATE PREPARE hotel_hinhanh_stmt;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Repair migrations keep the column to avoid removing data on rollback.
        }
    }
}
