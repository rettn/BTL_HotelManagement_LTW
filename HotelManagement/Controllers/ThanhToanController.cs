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
            // Ô API Key thường được hiển thị dạng password và có thể không gửi
            // lại giá trị khi người dùng chỉ sửa thông tin ngân hàng.
            // Giữ lại key cũ nếu người dùng không nhập key mới.
            if (string.IsNullOrWhiteSpace(model.SePayApiKey))
            {
                model.SePayApiKey = _settingService.GetSettings().SePayApiKey;
            }

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