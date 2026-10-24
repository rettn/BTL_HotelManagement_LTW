
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
    public class PhongRepository
    {
        private readonly ApplicationDbContext _context;

        public PhongRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lay danh sach phong kem loai phong
        public async Task<List<Phong>> GetAllAsync()
        {
            return await _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .OrderBy(p => p.SoPhong)
                .ToListAsync();
        }

        // Tim phong theo ma
        public async Task<Phong?> GetByIdAsync(int id)
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(p => p.MaPhong == id);
        }

        // Kiem tra so phong da ton tai
        public async Task<bool> ExistsSoPhongAsync(
            string soPhong, int? excludeId = null)
        {
            var query = _context.Phongs
                .AsNoTracking()
                .Where(p => p.SoPhong == soPhong);

            if (excludeId.HasValue)
            {
                query = query.Where(
                    p => p.MaPhong != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        // Them phong
        public async Task AddAsync(Phong phong)
        {
            _context.Phongs.Add(phong);
            await _context.SaveChangesAsync();
        }

        // Lay danh sach loai phong de hien thi
        public async Task<List<LoaiPhong>> GetLoaiPhongsAsync()
        {
            return await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(lp => lp.TenLoaiPhong)
                .ToListAsync();
        }
    }
}
