
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
    public class LoaiPhongRepository
    {
        private readonly ApplicationDbContext _context;

        public LoaiPhongRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lay danh sach loai phong
        public async Task<List<LoaiPhong>> GetAllAsync()
        {
            return await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(lp => lp.MaLoaiPhong)
                .ToListAsync();
        }

        // Tim loai phong theo ma
        public async Task<LoaiPhong?> GetByIdAsync(int id)
        {
            return await _context.LoaiPhongs
                .FirstOrDefaultAsync(lp => lp.MaLoaiPhong == id);
        }

        // Them loai phong
        public async Task AddAsync(LoaiPhong loaiPhong)
        {
            _context.LoaiPhongs.Add(loaiPhong);
            await _context.SaveChangesAsync();
        }

        // Cap nhat loai phong
        public async Task UpdateAsync(LoaiPhong loaiPhong)
        {
            _context.LoaiPhongs.Update(loaiPhong);
            await _context.SaveChangesAsync();
        }

        // Xoa loai phong
        public async Task<bool> DeleteAsync(int id)
        {
            var loaiPhong = await _context.LoaiPhongs
                .FirstOrDefaultAsync(lp => lp.MaLoaiPhong == id);

            if (loaiPhong == null)
                return false;

            bool dangCoPhong = await _context.Phongs
                .AnyAsync(p => p.MaLoaiPhong == id);

            if (dangCoPhong)
                return false;

            _context.LoaiPhongs.Remove(loaiPhong);
            await _context.SaveChangesAsync();

            return true;
        }

        // Tim kiem theo ten
        public async Task<List<LoaiPhong>> SearchAsync(string? keyword)
        {
            var query = _context.LoaiPhongs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(lp =>
                    lp.TenLoaiPhong.Contains(keyword));
            }

            return await query
                .OrderBy(lp => lp.TenLoaiPhong)
                .ToListAsync();
        }
    }
}
