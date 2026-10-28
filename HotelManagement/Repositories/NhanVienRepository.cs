
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
    public class NhanVienRepository
    {
        private readonly ApplicationDbContext _context;

        public NhanVienRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<NhanVien>> GetAllAsync(string? search = null)
        {
            var query = _context.NhanViens.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(n =>
                    n.HoTen.Contains(search) ||
                    n.ChucVu.Contains(search) ||
                    (n.Email != null && n.Email.Contains(search)) ||
                    (n.SoDienThoai != null &&
                     n.SoDienThoai.Contains(search)));
            }

            return await query
                .OrderBy(n => n.MaNV)
                .ToListAsync();
        }

        public Task<NhanVien?> GetByIdAsync(int id)
        {
            return _context.NhanViens
                .FirstOrDefaultAsync(n => n.MaNV == id);
        }

        public async Task AddAsync(NhanVien nhanVien)
        {
            _context.NhanViens.Add(nhanVien);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NhanVien nhanVien)
        {
            _context.NhanViens.Update(nhanVien);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.NhanViens
                .AnyAsync(n => n.MaNV == id);
        }

        public async Task<(int Tong, int DangLam, int DaNghi)>
            GetStatisticsAsync()
        {
            var tong = await _context.NhanViens.CountAsync();
            var dangLam = await _context.NhanViens
                .CountAsync(n => n.TrangThai);
            var daNghi = tong - dangLam;

            return (tong, dangLam, daNghi);
        }
    }
}
