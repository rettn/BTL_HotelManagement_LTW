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

        // Phuc vu tinh nang tim va loc
        public async Task<IEnumerable<Phong>> SearchAndFilterAsync(string keyword, int? maLoaiPhong, string trangThai)
        {
            var query = _context.Phongs.Include(p => p.LoaiPhong).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.SoPhong.Contains(keyword));
            }

            if (maLoaiPhong.HasValue && maLoaiPhong.Value > 0)
            {
                query = query.Where(p => p.MaLoaiPhong == maLoaiPhong.Value);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(p => p.TrangThai != null && p.TrangThai.Trim().ToLower() == trangThai.Trim().ToLower());
            }

            return await query.ToListAsync();
        }

        //Phuc vu chuc nang xoa
        public async Task<bool> KiemTraPhongDangDuocDatAsync(int maPhong)
        {
            return await _context.ChiTietDatPhongs
                .Include(ct => ct.DatPhong)
                .AnyAsync(ct => ct.MaPhong == maPhong
                    && ct.DatPhong != null
                    && ct.DatPhong.TrangThai != "DaHuy");
        }

        // Lay ds kem loai phong
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

        // Kiem tra so phong ton tai
        public async Task<bool> ExistsSoPhongAsync(string soPhong, int? excludeId = null)
        {
            var query = _context.Phongs
                .AsNoTracking()
                .Where(p => p.SoPhong == soPhong);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.MaPhong != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        // Them phong
        public async Task AddAsync(Phong phong)
        {
            _context.Phongs.Add(phong);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LoaiPhong>> GetLoaiPhongsAsync()
        {
            return await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(lp => lp.TenLoaiPhong)
                .ToListAsync();
        }

        //Cap nhat thong tin phogn
        public async Task<bool> UpdateAsync(Phong phong)
        {
            var existingPhong = await _context.Phongs.FindAsync(phong.MaPhong);
            if (existingPhong == null) return false;

            existingPhong.SoPhong = phong.SoPhong;
            existingPhong.MaLoaiPhong = phong.MaLoaiPhong;
            existingPhong.TrangThai = phong.TrangThai;
            existingPhong.GhiChu = phong.GhiChu;

            await _context.SaveChangesAsync();
            return true;
        }

        // Xoa phong
        public async Task<bool> DeleteAsync(int id)
        {
            var phong = await _context.Phongs.FindAsync(id);
            if (phong == null) return false;

            _context.Phongs.Remove(phong);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}