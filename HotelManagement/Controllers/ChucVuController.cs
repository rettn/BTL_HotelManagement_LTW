
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ChucVuController : Controller
    {
        private readonly ChucVuService _service;

        public ChucVuController(ChucVuService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _service.GetAllAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("TenChucVu,MoTa,TrangThai")] ChucVu chucVu)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thong tin chuc vu khong hop le.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _service.AddAsync(chucVu);
                TempData["Success"] = "Them chuc vu thanh cong.";
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind("MaChucVu,TenChucVu,MoTa,TrangThai")] ChucVu chucVu)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thong tin chuc vu khong hop le.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var updated = await _service.UpdateAsync(chucVu);

                if (!updated)
                    return NotFound();

                TempData["Success"] = "Cap nhat chuc vu thanh cong.";
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);

                if (!deleted)
                    return NotFound();

                TempData["Success"] = "Xoa chuc vu thanh cong.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
