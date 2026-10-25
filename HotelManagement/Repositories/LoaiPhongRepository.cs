
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

        public async Task<List<LoaiPhong>> GetAllAsync()
        {
            return await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(lp => lp.MaLoaiPhong)
                .ToListAsync();
        }

        public async Task<LoaiPhong?> GetByIdAsync(int id)
        {
            return await _context.LoaiPhongs
                .FirstOrDefaultAsync(lp => lp.MaLoaiPhong == id);
        }

        public async Task<bool> ExistsNameAsync(
            string tenLoaiPhong, int? excludeId = null)
        {
            string ten = tenLoaiPhong.Trim().ToLower();

            return await _context.LoaiPhongs.AnyAsync(lp =>
                lp.TenLoaiPhong.ToLower() == ten &&
                (!excludeId.HasValue ||
                 lp.MaLoaiPhong != excludeId.Value));
        }

        public async Task AddAsync(LoaiPhong loaiPhong)
        {
            _context.LoaiPhongs.Add(loaiPhong);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LoaiPhong loaiPhong)
        {
            _context.LoaiPhongs.Update(loaiPhong);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsBeingUsedAsync(int id)
        {
            return await _context.Phongs
                .AnyAsync(p => p.MaLoaiPhong == id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var loaiPhong = await _context.LoaiPhongs
                .FirstOrDefaultAsync(lp => lp.MaLoaiPhong == id);

            if (loaiPhong == null)
                return false;

            // Khong xoa neu van co phong thuoc loai nay.
            if (await IsBeingUsedAsync(id))
                return false;

            _context.LoaiPhongs.Remove(loaiPhong);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<LoaiPhong>> SearchAsync(string? keyword)
        {
            var query = _context.LoaiPhongs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string tuKhoa = keyword.Trim().ToLower();

                query = query.Where(lp =>
                    lp.TenLoaiPhong.ToLower().Contains(tuKhoa) ||
                    (lp.MoTa != null &&
                     lp.MoTa.ToLower().Contains(tuKhoa)));
            }

            return await query
                .OrderBy(lp => lp.MaLoaiPhong)
                .ToListAsync();
        }
    }
}
