
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
    public class ThuongRepository
    {
        private readonly ApplicationDbContext _context;

        public ThuongRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Thuong>> GetByMonthAsync(int thang, int nam)
        {
            return await _context.Thuongs
                .AsNoTracking()
                .Include(t => t.NhanVien)
                .Where(t => t.Thang == thang && t.Nam == nam)
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();
        }

        public async Task<Thuong?> GetByIdAsync(int id)
        {
            return await _context.Thuongs
                .Include(t => t.NhanVien)
                .FirstOrDefaultAsync(t => t.MaThuong == id);
        }

        public async Task AddAsync(Thuong thuong)
        {
            _context.Thuongs.Add(thuong);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Thuong thuong)
        {
            _context.Thuongs.Update(thuong);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Thuong thuong)
        {
            _context.Thuongs.Remove(thuong);
            await _context.SaveChangesAsync();
        }
    }
}
