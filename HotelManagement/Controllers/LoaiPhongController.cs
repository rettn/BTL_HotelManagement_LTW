
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LoaiPhongController : Controller
    {
        private readonly LoaiPhongService _service;

        public LoaiPhongController(LoaiPhongService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index(string? keyword)
        {
            var danhSach = string.IsNullOrWhiteSpace(keyword)
                ? await _service.GetAllAsync()
                : await _service.SearchAsync(keyword);

            ViewBag.Keyword = keyword;
            return View(danhSach);
        }

        // Danh sach dung trong modal cua trang so do phong.
        [HttpGet]
        public async Task<IActionResult> Popup(string? keyword)
        {
            var danhSach = string.IsNullOrWhiteSpace(keyword)
                ? await _service.GetAllAsync()
                : await _service.SearchAsync(keyword);

            ViewBag.Keyword = keyword;
            return PartialView("_QuanLyLoaiPhong", danhSach);
        }

        public async Task<IActionResult> Details(int id)
        {
            var loaiPhong = await _service.GetByIdAsync(id);

            if (loaiPhong == null)
                return NotFound();

            return View(loaiPhong);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new LoaiPhong());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoaiPhong loaiPhong)
        {
            if (!ModelState.IsValid)
                return View(loaiPhong);

            var ketQua = await _service.AddAsync(loaiPhong);

            if (!ketQua.ThanhCong)
            {
                ModelState.AddModelError("", ketQua.ThongBao);
                return View(loaiPhong);
            }

            TempData["SuccessMessage"] = ketQua.ThongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var loaiPhong = await _service.GetByIdAsync(id);

            if (loaiPhong == null)
                return NotFound();

            return View(loaiPhong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, LoaiPhong loaiPhong)
        {
            if (id != loaiPhong.MaLoaiPhong)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(loaiPhong);

            var ketQua = await _service.UpdateAsync(loaiPhong);

            if (!ketQua.ThanhCong)
            {
                ModelState.AddModelError("", ketQua.ThongBao);
                return View(loaiPhong);
            }

            TempData["SuccessMessage"] = ketQua.ThongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var loaiPhong = await _service.GetByIdAsync(id);

            if (loaiPhong == null)
                return NotFound();

            return View(loaiPhong);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ketQua = await _service.DeleteAsync(id);

            TempData[ketQua.ThanhCong
                ? "SuccessMessage"
                : "ErrorMessage"] = ketQua.ThongBao;

            return RedirectToAction(nameof(Index));
        }
    }
}
