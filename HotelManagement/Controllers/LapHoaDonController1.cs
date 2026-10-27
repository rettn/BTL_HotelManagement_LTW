
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    public class LapHoaDonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LapHoaDonService _service;

        public LapHoaDonController(
            ApplicationDbContext context,
            LapHoaDonService service)
        {
            _context = context;
            _service = service;
        }

        private async Task NapDanhSachLuuTru(LapHoaDonViewModel model)
        {
            var danhSach = await _context.LuuTrus
                .Include(l => l.ChiTietDatPhong)
                    .ThenInclude(c => c!.Phong)
                .Include(l => l.ChiTietDatPhong)
                    .ThenInclude(c => c!.DatPhong)
                        .ThenInclude(d => d!.KhachHang)
                .Where(l => l.ThoiGianNhanPhong != null
                         && l.ThoiGianTraPhong != null)
                .ToListAsync();

            model.DanhSachLuuTru = danhSach.Select(l =>
                new SelectListItem
                {
                    Value = l.MaLuuTru.ToString(),
                    Text = $"Lượt #{l.MaLuuTru} - " +
                           $"Phòng {l.ChiTietDatPhong?.Phong?.SoPhong} - " +
                           $"Khách {l.ChiTietDatPhong?.DatPhong?.KhachHang?.HoTen}"
                }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new LapHoaDonViewModel();
            await NapDanhSachLuuTru(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LapHoaDonViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await NapDanhSachLuuTru(model);
                return View(model);
            }

            try
            {
                var (success, message) =
                    await _service.LapHoaDonAsync(model);

                if (!success)
                {
                    ModelState.AddModelError(string.Empty, message);
                    await NapDanhSachLuuTru(model);
                    return View(model);
                }

                TempData["ThongBao"] = message;
                return RedirectToAction(nameof(Create));
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Có lỗi khi lập hóa đơn. Anh hãy kiểm tra dữ liệu và thử lại.");

                await NapDanhSachLuuTru(model);
                return View(model);
            }
        }

        public async Task<IActionResult> Index()
        {
            var danhSachHoaDon = await _context.HoaDons
                .Include(h => h.DatPhong)
                    .ThenInclude(d => d!.KhachHang)
                .OrderByDescending(h => h.NgayLap)
                .ToListAsync();

            return View(danhSachHoaDon);
        }


        [HttpGet]
        public async Task<IActionResult> Preview(int maLuuTru, decimal giamGia = 0)
        {
            var model = await _service.XemTruocAsync(maLuuTru, giamGia);

            if (model == null)
            {
                TempData["Loi"] =
                    "Không thể xem trước hóa đơn. Hãy kiểm tra lượt lưu trú và giảm giá.";

                return RedirectToAction(nameof(Create));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(
            int maLuuTru,
            decimal giamGia)
        {
            var model = new LapHoaDonViewModel
            {
                MaLuuTru = maLuuTru,
                GiamGia = giamGia
            };

            try
            {
                var (success, message) = await _service.LapHoaDonAsync(model);

                if (success)
                {
                    TempData["ThongBao"] = message;
                    return RedirectToAction(nameof(Create));
                }

                TempData["Loi"] = message;
                return RedirectToAction(nameof(Preview),
                    new { maLuuTru, giamGia });
            }
            catch (Exception)
            {
                TempData["Loi"] = "Có lỗi khi lập hóa đơn. Vui lòng thử lại.";
                return RedirectToAction(nameof(Preview),
                    new { maLuuTru, giamGia });
            }
        }

    }
}
