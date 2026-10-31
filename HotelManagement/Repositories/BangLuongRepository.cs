
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
    public class BangLuongRepository
    {
        private readonly ApplicationDbContext _context;

        public BangLuongRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BangLuong>> GetByMonthAsync(int thang, int nam)
        {
            return await _context.BangLuongs
                .AsNoTracking()
                .Include(b => b.NhanVien)
                .Where(b => b.Thang == thang && b.Nam == nam)
                .OrderBy(b => b.MaNV)
                .ToListAsync();
        }

        public async Task<BangLuong?> GetByIdAsync(int id)
        {
            return await _context.BangLuongs
                .Include(b => b.NhanVien)
                .FirstOrDefaultAsync(b => b.MaBangLuong == id);
        }

        public Task<BangLuong?> GetByEmployeeMonthAsync(
            int maNV, int thang, int nam)
        {
            return _context.BangLuongs.FirstOrDefaultAsync(b =>
                b.MaNV == maNV &&
                b.Thang == thang &&
                b.Nam == nam);
        }

        public async Task<List<NhanVien>> GetActiveEmployeesAsync()
        {
            return await _context.NhanViens
                .AsNoTracking()
                .Where(n => n.TrangThai)
                .OrderBy(n => n.MaNV)
                .ToListAsync();
        }

        public async Task AddAsync(BangLuong bangLuong)
        {
            _context.BangLuongs.Add(bangLuong);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BangLuong bangLuong)
        {
            _context.BangLuongs.Update(bangLuong);
            await _context.SaveChangesAsync();
        }

        public async Task<(int TongNhanVien, decimal TongLuong)>
            GetStatisticsAsync(int thang, int nam)
        {
            var query = _context.BangLuongs
                .Where(b => b.Thang == thang && b.Nam == nam);

            var tongNhanVien = await query.CountAsync();
            var tongLuong = await query.SumAsync(b => (decimal?)b.ThucLinh) ?? 0;

            return (tongNhanVien, tongLuong);
        }
    }
}
