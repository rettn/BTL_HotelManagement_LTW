
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
    public class PhuCapRepository
    {
        private readonly ApplicationDbContext _context;

        public PhuCapRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PhuCap>> GetByMonthAsync(int thang, int nam)
        {
            return await _context.PhuCaps
                .AsNoTracking()
                .Include(p => p.NhanVien)
                .Where(p => p.Thang == thang && p.Nam == nam)
                .OrderByDescending(p => p.NgayTao)
                .ToListAsync();
        }

        public async Task<PhuCap?> GetByIdAsync(int id)
        {
            return await _context.PhuCaps
                .Include(p => p.NhanVien)
                .FirstOrDefaultAsync(p => p.MaPhuCap == id);
        }

        public async Task AddAsync(PhuCap phuCap)
        {
            _context.PhuCaps.Add(phuCap);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PhuCap phuCap)
        {
            _context.PhuCaps.Update(phuCap);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(PhuCap phuCap)
        {
            _context.PhuCaps.Remove(phuCap);
            await _context.SaveChangesAsync();
        }
    }
}
