
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
    public class DatPhongRepository
    {
        private readonly ApplicationDbContext _context;

        public DatPhongRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DatPhong>> GetAllAsync()
        {
            return await _context.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
        }

        public async Task<DatPhong?> GetByIdAsync(int id)
        {
            return await _context.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == id);
        }

        public async Task<List<KhachHang>> GetKhachHangsAsync()
        {
            return await _context.KhachHangs
                .OrderBy(k => k.HoTen)
                .ToListAsync();
        }

        public async Task<List<Phong>> GetPhongsAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .OrderBy(p => p.SoPhong)
                .ToListAsync();
        }

        public async Task<bool> KhachHangTonTaiAsync(int maKH)
        {
            return await _context.KhachHangs
                .AnyAsync(k => k.MaKH == maKH);
        }

        public async Task<bool> PhongTonTaiAsync(int maPhong)
        {
            return await _context.Phongs
                .AnyAsync(p => p.MaPhong == maPhong);
        }

        public async Task<bool> PhongBiTrungLichAsync(
            int maPhong,
            DateTime ngayNhan,
            DateTime ngayTra,
            int? boQuaMaDatPhong = null)
        {
            return await _context.ChiTietDatPhongs
                .AnyAsync(ct =>
                    ct.MaPhong == maPhong &&
                    ct.NgayNhan < ngayTra &&
                    ct.NgayTra > ngayNhan &&
                    ct.DatPhong != null &&
                    ct.DatPhong.TrangThai != "DaHuy" &&
                    ct.DatPhong.TrangThai != "DaThanhToan" &&
                    ct.DatPhong.TrangThai != "DaHoanThanh" &&
                    (!ct.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null) ||
                     ct.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null && l.ThoiGianTraPhong == null)) &&
                    (boQuaMaDatPhong == null ||
                     ct.MaDatPhong != boQuaMaDatPhong.Value));
        }

        public async Task AddAsync(DatPhong datPhong)
        {
            _context.DatPhongs.Add(datPhong);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DatPhong datPhong)
        {
            _context.DatPhongs.Update(datPhong);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CoLuuTruAsync(int maDatPhong)
        {
            return await _context.LuuTrus
                .AnyAsync(l =>
                    l.ChiTietDatPhong != null &&
                    l.ChiTietDatPhong.MaDatPhong == maDatPhong);
        }

        public async Task<bool> CoHoaDonAsync(int maDatPhong)
        {
            return await _context.HoaDons
                .AnyAsync(h => h.MaDatPhong == maDatPhong);
        }
    }
}
