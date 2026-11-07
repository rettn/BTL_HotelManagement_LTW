using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
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
            [Bind("TenChucVu,MoTa,LuongCoBanMacDinh,TrangThai")] ChucVu chucVu)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin chức vụ không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _service.AddAsync(chucVu);
                TempData["Success"] = "Thêm chức vụ thành công.";
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
            [Bind("MaChucVu,TenChucVu,MoTa,LuongCoBanMacDinh,TrangThai")] ChucVu chucVu)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin chức vụ không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var updated = await _service.UpdateAsync(chucVu);
                if (!updated)
                    return NotFound();

                TempData["Success"] = "Cập nhật chức vụ thành công.";
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

                TempData["Success"] = "Xóa chức vụ thành công.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
