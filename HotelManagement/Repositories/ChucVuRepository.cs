
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
    public class ChucVuRepository
    {
        private readonly ApplicationDbContext _context;

        public ChucVuRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChucVu>> GetAllAsync()
        {
            return await _context.ChucVus
                .AsNoTracking()
                .OrderBy(c => c.TenChucVu)
                .ToListAsync();
        }

        public Task<ChucVu?> GetByIdAsync(int id)
        {
            return _context.ChucVus
                .FirstOrDefaultAsync(c => c.MaChucVu == id);
        }

        public Task<bool> ExistsByNameAsync(
            string tenChucVu, int? excludeId = null)
        {
            return _context.ChucVus.AnyAsync(c =>
                c.TenChucVu.ToLower() == tenChucVu.ToLower()
                && (!excludeId.HasValue || c.MaChucVu != excludeId.Value));
        }

        public async Task AddAsync(ChucVu chucVu)
        {
            _context.ChucVus.Add(chucVu);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ChucVu chucVu)
        {
            _context.ChucVus.Update(chucVu);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsInUseAsync(string tenChucVu)
        {
            return await _context.NhanViens.AnyAsync(n =>
                n.ChucVu == tenChucVu);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var chucVu = await GetByIdAsync(id);

            if (chucVu == null)
                return false;

            // Khong xoa neu dang co nhan vien su dung chuc vu nay.
            if (await IsInUseAsync(chucVu.TenChucVu))
                throw new InvalidOperationException(
                    "Chức vụ đang được nhân viên sử dụng, không được xóa.");

            _context.ChucVus.Remove(chucVu);
            await _context.SaveChangesAsync();

            return true;
        }

        public Task<List<NhanVien>> GetNhanViensByChucVuAsync(string tenChucVu)
        {
            return _context.NhanViens
                .Where(n => n.ChucVu == tenChucVu)
                .ToListAsync();
        }
        public async Task UpdateNhanViensAsync(
            List<NhanVien> nhanViens)
        {
            await _context.SaveChangesAsync();
        }
    }
}
