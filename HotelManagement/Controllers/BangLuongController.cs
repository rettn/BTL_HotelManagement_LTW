
using HotelManagement.Models;
using HotelManagement.Services;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class BangLuongController : Controller
    {
        private readonly BangLuongService _service;

        public BangLuongController(BangLuongService service)
        {
            _service = service;
        }

        // GET: /BangLuong?thang=10&nam=2026
        public async Task<IActionResult> Index(int? thang, int? nam)
        {
            int thangHienTai = thang ?? DateTime.Today.Month;
            int namHienTai = nam ?? DateTime.Today.Year;

            if (thangHienTai < 1 || thangHienTai > 12 ||
                namHienTai < 2000 || namHienTai > 2100)
            {
                TempData["Error"] = "Tháng hoặc năm không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var danhSach = await _service.GetByMonthAsync(
                thangHienTai, namHienTai);

            var thongKe = await _service.GetStatisticsAsync(
                thangHienTai, namHienTai);

            return View(new BangLuongIndexViewModel
            {
                BangLuongs = danhSach,
                NhanViens = await _service.GetActiveEmployeesAsync(),
                Thang = thangHienTai,
                Nam = namHienTai,
                TongNhanVien = thongKe.TongNhanVien,
                TongLuong = thongKe.TongLuong
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculate(int thang, int nam, int ngayCongQuyDinh, Dictionary<int, decimal> ngayCongThucTe)
        {
            try
            {
                var result = await _service.CalculateAsync(thang, nam, ngayCongQuyDinh, ngayCongThucTe ?? new());
                TempData[result.Success ? "Success" : "Error"] = result.Message;
            }
            catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Index), new { thang, nam });
        }

        // GET: /BangLuong/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var bangLuong = await _service.GetByIdAsync(id);

            if (bangLuong == null)
                return NotFound();

            return View(bangLuong);
        }
    }
}
