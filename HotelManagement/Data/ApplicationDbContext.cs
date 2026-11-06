
using HotelManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<ChucVu> ChucVus { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<LoaiPhong> LoaiPhongs { get; set; }
        public DbSet<Phong> Phongs { get; set; }
        public DbSet<DatPhong> DatPhongs { get; set; }
        public DbSet<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public DbSet<LuuTru> LuuTrus { get; set; }
        public DbSet<DichVu> DichVus { get; set; }
        public DbSet<PhieuNhapKho> PhieuNhapKhos { get; set; }
        public DbSet<ChiTietNhapKho> ChiTietNhapKhos { get; set; }
        public DbSet<ChiTietDichVu> ChiTietDichVus { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<ThanhToan> ThanhToans { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }
        public DbSet<NhatKyHeThong> NhatKyHeThongs { get; set; }
        public DbSet<LichSuPhong> LichSuPhongs { get; set; }
        public DbSet<HoanTien> HoanTiens { get; set; }
        public DbSet<BangLuong> BangLuongs { get; set; }
        public DbSet<Thuong> Thuongs { get; set; }
        public DbSet<PhuCap> PhuCaps { get; set; }
        public DbSet<KhauTru> KhauTrus { get; set; }
        public DbSet<CaiDatHeThong> CaiDatHeThongs { get; set; }
        protected override void OnModelCreating(
            Microsoft.EntityFrameworkCore.ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<HotelManagement.Models.NhanVien>()
                .HasIndex(n => n.IdentityUserId)
                .IsUnique();

            builder.Entity<BangLuong>()
        .HasOne(b => b.NhanVien)
        .WithMany()
        .HasForeignKey(b => b.MaNV)
        .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BangLuong>()
                .HasIndex(b => new { b.MaNV, b.Thang, b.Nam })
                .IsUnique();

            builder.Entity<Thuong>()
                .HasOne(t => t.NhanVien)
                .WithMany()
                .HasForeignKey(t => t.MaNV)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PhuCap>()
                .HasOne(p => p.NhanVien)
                .WithMany()
                .HasForeignKey(p => p.MaNV)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<KhauTru>()
                .HasOne(k => k.NhanVien)
                .WithMany()
                .HasForeignKey(k => k.MaNV)
                .OnDelete(DeleteBehavior.Restrict);
        }


    }


}
