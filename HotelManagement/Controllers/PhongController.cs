
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    public class PhongController : Controller
    {
        private readonly PhongService _service;

        public PhongController(PhongService service)
        {
            _service = service;
        }

        // GET: /Phong
        public async Task<IActionResult> Index()
        {
            var danhSachPhong = await _service.GetAllAsync();

            return View(danhSachPhong);
        }

        // GET: /Phong/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var phong = await _service.GetByIdAsync(id);

            if (phong == null)
                return NotFound();

            return Json(new
            {
                maPhong = phong.MaPhong,
                soPhong = phong.SoPhong,
                loaiPhong = phong.LoaiPhong?.TenLoaiPhong ?? "Chưa phân loại",
                giaTheoDem = phong.LoaiPhong?.GiaTheoDem ?? 0,
                trangThai = phong.TrangThai,
                ghiChu = phong.GhiChu
            });
        }

        // GET: /Phong/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.DanhSachLoaiPhong =
                await _service.GetLoaiPhongsAsync();

            return View(new Phong());
        }

        // POST: /Phong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Phong phong)
        {
            // Loai bo validation cho navigation property
            ModelState.Remove(nameof(Phong.LoaiPhong));
            ModelState.Remove(nameof(Phong.ChiTietDatPhongs));
            ModelState.Remove(nameof(Phong.LichSuPhongs));

            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachLoaiPhong =
                    await _service.GetLoaiPhongsAsync();

                return View(phong);
            }

            var ketQua = await _service.AddAsync(phong);

            if (!ketQua.ThanhCong)
            {
                ModelState.AddModelError(
                    string.Empty, ketQua.ThongBao);

                ViewBag.DanhSachLoaiPhong =
                    await _service.GetLoaiPhongsAsync();

                return View(phong);
            }

            TempData["SuccessMessage"] = ketQua.ThongBao;

            return RedirectToAction(nameof(Index));
        }
    }
}
