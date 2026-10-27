
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
    public class KhachHangRepository
    {
        private readonly ApplicationDbContext _context;

        public KhachHangRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<KhachHang>> GetAllAsync(string? keyword = null)
        {
            var query = _context.KhachHangs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(k =>
                    k.HoTen.Contains(keyword) ||
                    (k.SoDienThoai != null &&
                     k.SoDienThoai.Contains(keyword)) ||
                    (k.Email != null &&
                     k.Email.Contains(keyword)) ||
                    (k.CCCD != null &&
                     k.CCCD.Contains(keyword)));
            }

            return await query
                .OrderBy(k => k.MaKH)
                .ToListAsync();
        }

        public async Task<KhachHang?> GetByIdAsync(int id)
        {
            return await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.MaKH == id);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.KhachHangs
                .AnyAsync(k => k.MaKH == id);
        }

        public async Task<bool> HasBookingAsync(int id)
        {
            return await _context.DatPhongs
                .AnyAsync(d => d.MaKH == id);
        }

        public async Task AddAsync(KhachHang khachHang)
        {
            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(KhachHang khachHang)
        {
            _context.KhachHangs.Update(khachHang);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(KhachHang khachHang)
        {
            _context.KhachHangs.Remove(khachHang);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsPhoneUsedAsync(
            string phone, int? excludeId = null)
        {
            return await _context.KhachHangs.AnyAsync(k =>
                k.SoDienThoai == phone &&
                (excludeId == null || k.MaKH != excludeId.Value));
        }

        public async Task<bool> IsEmailUsedAsync(
            string email, int? excludeId = null)
        {
            return await _context.KhachHangs.AnyAsync(k =>
                k.Email == email &&
                (excludeId == null || k.MaKH != excludeId.Value));
        }

        public async Task<bool> IsCCCDUsedAsync(
            string cccd, int? excludeId = null)
        {
            return await _context.KhachHangs.AnyAsync(k =>
                k.CCCD == cccd &&
                (excludeId == null || k.MaKH != excludeId.Value));
        }

        public async Task<KhachHang?> GetByIdWithBookingsAsync(int id)
        {
            return await _context.KhachHangs
                .AsNoTracking()
                .Include(k => k.DanhSachDatPhong)
                    .ThenInclude(dp => dp.ChiTietDatPhongs)
                        .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(k => k.MaKH == id);
        }
    }
}
