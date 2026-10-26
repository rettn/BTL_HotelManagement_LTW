using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DichVuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DichVuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DichVu
        public async Task<IActionResult> Index(string keyword)
        {
            ViewBag.Keyword = keyword;
            var query = _context.DichVus.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(d => d.TenDichVu.Contains(keyword));
            }

            return View(await query.ToListAsync());
        }

        // GET: DichVu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var dichVu = await _context.DichVus.FirstOrDefaultAsync(m => m.Id == id);
            if (dichVu == null) return NotFound();

            return View(dichVu);
        }

        // GET: DichVu/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DichVu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TenDichVu,MoTa,Gia,TrangThai")] DichVu dichVu)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dichVu);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm dịch vụ thành công!";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Có lỗi xảy ra, vui lòng kiểm tra lại dữ liệu.";
            return View(dichVu);
        }

        // GET: DichVu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null) return NotFound();
            return View(dichVu);
        }

        // POST: DichVu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TenDichVu,MoTa,Gia,TrangThai")] DichVu dichVu)
        {
            if (id != dichVu.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dichVu);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật dịch vụ thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DichVuExists(dichVu.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Cập nhật thất bại, vui lòng kiểm tra lại.";
            return View(dichVu);
        }

        // GET: DichVu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var dichVu = await _context.DichVus.FirstOrDefaultAsync(m => m.Id == id);
            if (dichVu == null) return NotFound();

            return View(dichVu);
        }

        // POST: DichVu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu != null)
            {
                _context.DichVus.Remove(dichVu);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa dịch vụ thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ để xóa.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DichVuExists(int id)
        {
            return _context.DichVus.Any(e => e.Id == id);
        }
    }
}