
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
        public DbSet<ChiTietDichVu> ChiTietDichVus { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<ThanhToan> ThanhToans { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }
        public DbSet<NhatKyHeThong> NhatKyHeThongs { get; set; }
        public DbSet<LichSuPhong> LichSuPhongs { get; set; }
        public DbSet<HoanTien> HoanTiens { get; set; }
        protected override void OnModelCreating(
            Microsoft.EntityFrameworkCore.ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<HotelManagement.Models.NhanVien>()
                .HasIndex(n => n.IdentityUserId)
                .IsUnique();
        }


    }


}
