
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
    public class KhauTruRepository
    {
        private readonly ApplicationDbContext _context;

        public KhauTruRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<KhauTru>> GetByMonthAsync(int thang, int nam)
        {
            return await _context.KhauTrus
                .AsNoTracking()
                .Include(k => k.NhanVien)
                .Where(k => k.Thang == thang && k.Nam == nam)
                .OrderByDescending(k => k.NgayTao)
                .ToListAsync();
        }

        public async Task<KhauTru?> GetByIdAsync(int id)
        {
            return await _context.KhauTrus
                .Include(k => k.NhanVien)
                .FirstOrDefaultAsync(k => k.MaKhauTru == id);
        }

        public async Task AddAsync(KhauTru khauTru)
        {
            _context.KhauTrus.Add(khauTru);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(KhauTru khauTru)
        {
            _context.KhauTrus.Update(khauTru);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(KhauTru khauTru)
        {
            _context.KhauTrus.Remove(khauTru);
            await _context.SaveChangesAsync();
        }
    }
}
