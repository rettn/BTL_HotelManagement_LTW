using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ThanhToanController : Controller
    {
        private readonly PaymentSettingService _settingService;
        private readonly SePayService _sePayService;

        public ThanhToanController(PaymentSettingService settingService, SePayService sePayService)
        {
            _settingService = settingService;
            _sePayService = sePayService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var settings = _settingService.GetSettings();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LuuCaiDatThanhToan(PaymentSetting model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra lại các trường thông tin cấu hình.";
                return View("Index", model);
            }

            _settingService.SaveSettings(model);
            TempData["SuccessMessage"] = "Đã lưu thông tin tài khoản ngân hàng và cấu hình SePay API thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KiemTraKetNoiSePay()
        {
            var message = await _sePayService.TestConnectionAsync();
            return Json(new { message });
        }
    }
}