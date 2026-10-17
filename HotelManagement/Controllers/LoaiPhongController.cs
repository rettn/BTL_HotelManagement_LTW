
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    public class LoaiPhongController : Controller
    {
        private readonly LoaiPhongService _service;

        public LoaiPhongController(LoaiPhongService service)
        {
            _service = service;
        }

        // GET: /LoaiPhong
        public async Task<IActionResult> Index(string? keyword)
        {
            var danhSach = string.IsNullOrWhiteSpace(keyword)
                ? await _service.GetAllAsync()
                : await _service.SearchAsync(keyword);

            ViewBag.Keyword = keyword;

            return View(danhSach);
        }

        // GET: /LoaiPhong/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var loaiPhong = await _service.GetByIdAsync(id);

            if (loaiPhong == null)
                return NotFound();

            return View(loaiPhong);
        }

        // GET: /LoaiPhong/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /LoaiPhong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoaiPhong loaiPhong)
        {
            if (!ModelState.IsValid)
                return View(loaiPhong);

            var ketQua = await _service.AddAsync(loaiPhong);

            if (!ketQua.ThanhCong)
            {
                ModelState.AddModelError(
                    string.Empty, ketQua.ThongBao);

                return View(loaiPhong);
            }

            TempData["SuccessMessage"] = ketQua.ThongBao;

            return RedirectToAction(nameof(Index));
        }

        // GET: /LoaiPhong/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var loaiPhong = await _service.GetByIdAsync(id);

            if (loaiPhong == null)
                return NotFound();

            return View(loaiPhong);
        }

        // POST: /LoaiPhong/Edit/5
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
                ModelState.AddModelError(
                    string.Empty, ketQua.ThongBao);

                return View(loaiPhong);
            }

            TempData["SuccessMessage"] = ketQua.ThongBao;

            return RedirectToAction(nameof(Index));
        }

        // GET: /LoaiPhong/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var loaiPhong = await _service.GetByIdAsync(id);

            if (loaiPhong == null)
                return NotFound();

            return View(loaiPhong);
        }

        // POST: /LoaiPhong/Delete/5
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
