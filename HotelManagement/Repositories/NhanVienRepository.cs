using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Extensions;
using HotelManagement.ViewModels;
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
            return await BuildQuery(search).ToListAsync();
        }

        public Task<PagedResult<NhanVien>> GetPageAsync(string? search, int page, int pageSize) =>
            BuildQuery(search).ToPagedResultAsync(page, pageSize);

        private IQueryable<NhanVien> BuildQuery(string? search)
        {
            var query = _context.NhanViens.AsNoTracking();
            if (string.IsNullOrWhiteSpace(search)) return query.OrderBy(n => n.MaNV);
            search = search.Trim();
            const string collation = "utf8mb4_bin";
            var maText = search.StartsWith("NV", StringComparison.OrdinalIgnoreCase) ? search.Substring(2) : search;
            var maNV = int.TryParse(maText, out var id) ? id : -1;
            var laEmail = search.Contains('@');
            var laSo = search.All(char.IsDigit);
            return query.Where(n => n.MaNV == maNV ||
                EF.Functions.Collate(n.HoTen, collation).Contains(search) ||
                EF.Functions.Collate(n.ChucVu, collation).Contains(search) ||
                (laEmail && n.Email != null && EF.Functions.Collate(n.Email, collation).Contains(search)) ||
                (laSo && n.SoDienThoai != null && n.SoDienThoai.Contains(search)))
                .OrderBy(n => n.MaNV);
        }

        public Task<NhanVien?> GetByIdAsync(int id)
        {
            return _context.NhanViens
                .FirstOrDefaultAsync(n => n.MaNV == id);
        }

        public Task<List<NhanVien>> GetActiveEmployeesAsync() => _context.NhanViens.AsNoTracking().Where(n => n.TrangThai).OrderBy(n => n.MaNV).ToListAsync();

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
