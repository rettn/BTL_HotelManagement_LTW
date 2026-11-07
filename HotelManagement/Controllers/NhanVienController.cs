using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelManagement.ViewModels;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
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
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var result = await _nhanVienService.GetPageAsync(search, page, pageSize);

            ViewBag.Search = search;
            ViewBag.Pagination = result.ToPagination(nameof(Index), "NhanVien", new Dictionary<string, string?> { ["search"] = search });

            var statistics = await _nhanVienService.GetStatisticsAsync();

            ViewBag.TongNhanVien = statistics.Tong;
            ViewBag.DangLam = statistics.DangLam;
            ViewBag.DaNghi = statistics.DaNghi;

            return View(result.Items);
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

                TempData["Success"] = "Thêm nhân viên thành công.";
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

                TempData["Success"] = "Cập nhật nhân viên thành công.";
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

            TempData["Success"] = "Cập nhật trạng thái nhân viên thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}
