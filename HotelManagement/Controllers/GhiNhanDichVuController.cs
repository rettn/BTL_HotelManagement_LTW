using HotelManagement.Services;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    public class GhiNhanDichVuController : Controller
    {
        private readonly GhiNhanDichVuService _service;

        public GhiNhanDichVuController(
            GhiNhanDichVuService service)
        {
            _service = service;
        }

        // Hiển thị danh sách dịch vụ cần ghi nhận
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new GhiNhanDichVuViewModel
            {
                DanhSachLuuTru =
                    await _service.GetDanhSachLuuTruAsync(),

                DanhSachDichVu =
                    await _service.GetDanhSachDichVuAsync()
            };

            return View(model);
        }

        // Lưu dịch vụ khách đã sử dụng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            GhiNhanDichVuViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.DanhSachLuuTru =
                    await _service.GetDanhSachLuuTruAsync();

                model.DanhSachDichVu =
                    await _service.GetDanhSachDichVuAsync();

                return View(model);
            }

            var result = await _service.CreateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    string.Empty, result.Message);

                model.DanhSachLuuTru =
                    await _service.GetDanhSachLuuTruAsync();

                model.DanhSachDichVu =
                    await _service.GetDanhSachDichVuAsync();

                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Create));
        }
    }
}