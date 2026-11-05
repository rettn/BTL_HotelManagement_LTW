using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuanLyKho : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhieuNhapKho",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaPhieu = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayNhap = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NhaCungCap = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GhiChu = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuNhapKho", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ChiTietNhapKho",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PhieuNhapKhoId = table.Column<int>(type: "int", nullable: false),
                    DichVuId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGiaNhap = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietNhapKho", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietNhapKho_DichVus_DichVuId",
                        column: x => x.DichVuId,
                        principalTable: "DichVus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietNhapKho_PhieuNhapKho_PhieuNhapKhoId",
                        column: x => x.PhieuNhapKhoId,
                        principalTable: "PhieuNhapKho",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietNhapKho_DichVuId",
                table: "ChiTietNhapKho",
                column: "DichVuId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietNhapKho_PhieuNhapKhoId",
                table: "ChiTietNhapKho",
                column: "PhieuNhapKhoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietNhapKho");

            migrationBuilder.DropTable(
                name: "PhieuNhapKho");
        }
    }
}
