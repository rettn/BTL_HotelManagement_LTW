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

        // ham ho tro tim va loc
        public async Task<IActionResult> Index(string? keyword, int? maLoaiPhong, string? trangThai)
        {
            var danhSachPhong = await _service.SearchAndFilterAsync(keyword, maLoaiPhong, trangThai);

            ViewBag.Keyword = keyword;
            ViewBag.MaLoaiPhong = maLoaiPhong;
            ViewBag.TrangThai = trangThai;
            ViewBag.DanhSachLoaiPhong = await _service.GetLoaiPhongsAsync();

            return View(danhSachPhong);
        }

        //GET: /Phong/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var phong = await _service.GetByIdAsync(id);
            if (phong == null)
                return NotFound();
            return View(phong);
        }

        // GET: /Phong/Popup/5
        [HttpGet]
        public async Task<IActionResult> Popup(int id)
        {
            var phong = await _service.GetByIdAsync(id);
            if (phong == null) return NotFound();

            return PartialView("_QuanLyThePhong", phong);
        }

        // GET: /Phong/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.DanhSachLoaiPhong = await _service.GetLoaiPhongsAsync();
            return View(new Phong());

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Phong phong)
        {
            ModelState.Remove(nameof(Phong.LoaiPhong));
            ModelState.Remove(nameof(Phong.ChiTietDatPhongs));
            ModelState.Remove(nameof(Phong.LichSuPhongs));

            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachLoaiPhong = await _service.GetLoaiPhongsAsync();
                return View(phong);
            }

            var ketQua = await _service.AddAsync(phong);

            if (!ketQua.ThanhCong)
            {
                ModelState.AddModelError(string.Empty, ketQua.ThongBao);
                ViewBag.DanhSachLoaiPhong = await _service.GetLoaiPhongsAsync();
                return View(phong);

            }
            TempData["SuccessMessage"] = ketQua.ThongBao;
            return RedirectToAction(nameof(Index));

        }
        // ham sua phong
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var phong = await _service.GetByIdAsync(id);

            if (phong == null) return NotFound();
            ViewBag.DanhSachLoaiPhong = await _service.GetLoaiPhongsAsync();
            return View(phong);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Phong phong)

        {

            if (id != phong.MaPhong) return BadRequest();

            ModelState.Remove(nameof(Phong.LoaiPhong));
            ModelState.Remove(nameof(Phong.ChiTietDatPhongs));
            ModelState.Remove(nameof(Phong.LichSuPhongs));

            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachLoaiPhong = await _service.GetLoaiPhongsAsync();
                return View(phong);

            }
            var ketQua = await _service.UpdateAsync(phong);
            if (!ketQua.ThanhCong)
            {
                ModelState.AddModelError(string.Empty, ketQua.ThongBao);
                ViewBag.DanhSachLoaiPhong = await _service.GetLoaiPhongsAsync();
                return View(phong);

            }
            TempData["SuccessMessage"] = ketQua.ThongBao;
            return RedirectToAction(nameof(Index));
        }

        // ham xoa phong
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var phong = await _service.GetByIdAsync(id);
            if (phong == null) return NotFound();
            return View(phong);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)

        {

            var ketQua = await _service.DeleteAsync(id);
            TempData[ketQua.ThanhCong ? "SuccessMessage" : "ErrorMessage"] = ketQua.ThongBao;
            return RedirectToAction(nameof(Index));

        }

    }

}