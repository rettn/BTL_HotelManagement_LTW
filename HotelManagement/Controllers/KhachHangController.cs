
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class KhachHangController : Controller
    {
        private readonly KhachHangService _service;

        public KhachHangController(KhachHangService service)
        {
            _service = service;
        }

        // GET: /KhachHang
        public async Task<IActionResult> Index(string? keyword)
        {
            var danhSach = await _service.GetAllAsync(keyword);

            ViewBag.Keyword = keyword;
            return View(danhSach);
        }

        // GET: /KhachHang/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var khachHang = await _service.GetByIdAsync(id);

            if (khachHang == null)
                return NotFound();

            return View(khachHang);
        }

        // GET: /KhachHang/Create
        public IActionResult Create()
        {
            return View(new KhachHang());
        }

        // POST: /KhachHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhachHang model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _service.CreateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: /KhachHang/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var khachHang = await _service.GetByIdAsync(id);

            if (khachHang == null)
                return NotFound();

            return View(khachHang);
        }

        // POST: /KhachHang/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhachHang model)
        {
            if (id != model.MaKH)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var result = await _service.UpdateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: /KhachHang/Delete/1
        public async Task<IActionResult> Delete(int id)
        {
            var khachHang = await _service.GetByIdAsync(id);

            if (khachHang == null)
                return NotFound();

            return View(khachHang);
        }

        // POST: /KhachHang/Delete/1
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result.Success)
                TempData["Error"] = result.Message;
            else
                TempData["Success"] = result.Message;

            return RedirectToAction(nameof(Index));
        }
    }
}
