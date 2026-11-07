
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class ThuongController : Controller
    {
        private readonly ThuongService _service;
        private readonly NhanVienService _nhanVienService;

        public ThuongController(ThuongService service, NhanVienService nhanVienService)
        {
            _service = service;
            _nhanVienService = nhanVienService;
        }

        private async Task LoadEmployeesAsync() => ViewBag.NhanViens = await _nhanVienService.GetAllAsync();

        public async Task<IActionResult> Index(int? thang, int? nam)
        {
            int t = thang ?? DateTime.Today.Month;
            int n = nam ?? DateTime.Today.Year;

            if (t < 1 || t > 12 || n < 2000 || n > 2100)
                return BadRequest("Tháng hoặc năm không hợp lệ.");

            ViewBag.Thang = t;
            ViewBag.Nam = n;
            ViewBag.TongTien = (await _service.GetByMonthAsync(t, n)).Sum(x => x.SoTien);

            return View(await _service.GetByMonthAsync(t, n));
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? thang, int? nam)
        {
            await LoadEmployeesAsync();
            return View(new Thuong
            {
                Thang = thang ?? DateTime.Today.Month,
                Nam = nam ?? DateTime.Today.Year,
                NgayTao = DateTime.Now
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Thuong model)
        {
            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return View(model);
            }

            try
            {
                var result = await _service.AddAsync(model);
                if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message); await LoadEmployeesAsync(); return View(model); }
                TempData["Success"] = "Thêm khoản thưởng thành công.";
                return RedirectToAction(nameof(Index),
                    new { thang = model.Thang, nam = model.Nam });
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Không thể thêm khoản thưởng.");
                await LoadEmployeesAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _service.GetByIdAsync(id);

            if (model == null)
                return NotFound();

            await LoadEmployeesAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Thuong model)
        {
            if (id != model.MaThuong)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return View(model);
            }

            try
            {
                var result = await _service.UpdateAsync(model);
                if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message); await LoadEmployeesAsync(); return View(model); }
                TempData["Success"] = "Cập nhật khoản thưởng thành công.";
                return RedirectToAction(nameof(Index),
                    new { thang = model.Thang, nam = model.Nam });
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Không thể cập nhật khoản thưởng.");
                await LoadEmployeesAsync();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _service.GetByIdAsync(id);

            if (model == null)
                return NotFound();

            int t = model.Thang;
            int n = model.Nam;

            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result.Success) TempData["Error"] = result.Message;
                else TempData["Success"] = result.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "Không thể xóa khoản thưởng.";
            }

            return RedirectToAction(nameof(Index),
                new { thang = t, nam = n });
        }
    }
}
