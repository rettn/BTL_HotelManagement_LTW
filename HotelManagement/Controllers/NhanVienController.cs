
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NhanVienController : Controller
    {
        private readonly ChucVuService _chucVuService;
        private readonly NhanVienService _nhanVienService;
        public NhanVienController(
            NhanVienService nhanVienService,
            ChucVuService chucVuService)
        {
            _nhanVienService = nhanVienService;
            _chucVuService = chucVuService;
        }

        private async Task LoadChucVuAsync()
        {
            ViewBag.ChucVus = await _chucVuService.GetAllAsync();
        }
        // Danh sach nhan vien
        public async Task<IActionResult> Index(string? search)
        {
            var nhanViens = await _nhanVienService.GetAllAsync(search);

            ViewBag.Search = search;

            var statistics = await _nhanVienService.GetStatisticsAsync();

            ViewBag.TongNhanVien = statistics.Tong;
            ViewBag.DangLam = statistics.DangLam;
            ViewBag.DaNghi = statistics.DaNghi;

            return View(nhanViens);
        }

        // Xem chi tiet
        public async Task<IActionResult> Details(int id)
        {
            var nhanVien = await _nhanVienService.GetByIdAsync(id);

            if (nhanVien == null)
                return NotFound();

            return View(nhanVien);
        }

        // Hien thi form them
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadChucVuAsync();

            return View(new NhanVien
            {
                NgayVaoLam = DateTime.Today,
                TrangThai = true
            });
        }


        // Xu ly them nhan vien
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien nhanVien)
        {
            if (!ModelState.IsValid)
            {
                await LoadChucVuAsync();
                return View(nhanVien);
            }

            try
            {
                await _nhanVienService.AddAsync(nhanVien);

                TempData["Success"] = "Them nhan vien thanh cong.";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(
                    nameof(nhanVien.LuongCoBan), ex.Message);

                await LoadChucVuAsync();
                return View(nhanVien);
            }
        }

        // Hien thi form sua

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var nhanVien = await _nhanVienService.GetByIdAsync(id);

            if (nhanVien == null)
                return NotFound();

            await LoadChucVuAsync();

            return View(nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien nhanVien)
        {
            if (id != nhanVien.MaNV)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadChucVuAsync();
                return View(nhanVien);
            }

            try
            {
                var updated = await _nhanVienService.UpdateAsync(nhanVien);

                if (!updated)
                    return NotFound();

                TempData["Success"] = "Cap nhat nhan vien thanh cong.";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(
                    nameof(nhanVien.LuongCoBan), ex.Message);

                await LoadChucVuAsync();
                return View(nhanVien);
            }
        }



        // Doi trang thai lam viec
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            var changed = await _nhanVienService.ChangeStatusAsync(id);

            if (!changed)
                return NotFound();

            TempData["Success"] = "Cap nhat trang thai nhan vien thanh cong.";

            return RedirectToAction(nameof(Index));
        }
    }
}
