using Microsoft.AspNetCore.Authorization;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class DatPhongController : Controller
    {
        private readonly DatPhongService _service;
        private readonly ApplicationDbContext _context;

        public DatPhongController(
            DatPhongService service,
            ApplicationDbContext context)
        {
            _service = service;
            _context = context;
        }

        // GET: /DatPhong
        public async Task<IActionResult> Index()
        {
            var danhSach = await _service.GetAllAsync();
            return View(danhSach);
        }

        // GET: /DatPhong/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var datPhong = await _service.GetByIdAsync(id);

            if (datPhong == null)
                return NotFound();

            return View(datPhong);
        }

        // GET: /DatPhong/Create
        public async Task<IActionResult> Create(int? maPhong)
        {
            var model = new DatPhongViewModel
            {
                NgayNhan = DateTime.Today,
                NgayTra = DateTime.Today.AddDays(1),
                MaPhongs = maPhong.HasValue
                    ? new List<int> { maPhong.Value }
                    : new List<int>()
            };

            await NapDuLieuFormAsync(model);
            return View(model);
        }


        // POST: /DatPhong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DatPhongViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await NapDuLieuFormAsync(model);
                return View(model);
            }

            var result = await _service.CreateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await NapDuLieuFormAsync(model);
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: /DatPhong/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var datPhong = await _service.GetByIdAsync(id);

            if (datPhong == null)
                return NotFound();

            if (datPhong.TrangThai != "ChoXacNhan")
            {
                TempData["Error"] =
                    "Chỉ có thể sửa phiếu đang chờ xác nhận.";

                return RedirectToAction(nameof(Index));
            }

            var chiTiet = datPhong.ChiTietDatPhongs.ToList();

            var model = new DatPhongViewModel
            {
                MaDatPhong = datPhong.MaDatPhong,
                MaKH = datPhong.MaKH,
                NgayNhan = chiTiet.Count > 0
                    ? chiTiet.Min(c => c.NgayNhan)
                    : datPhong.NgayNhanDuKien,
                NgayTra = chiTiet.Count > 0
                    ? chiTiet.Max(c => c.NgayTra)
                    : datPhong.NgayTraDuKien,
                TienCoc = datPhong.TienCoc,
                GhiChu = datPhong.GhiChu,
                MaPhongs = chiTiet.Select(c => c.MaPhong).ToList()
            };

            await NapDuLieuFormAsync(model);
            return View(model);
        }

        // POST: /DatPhong/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, DatPhongViewModel model)
        {
            if (id != model.MaDatPhong)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await NapDuLieuFormAsync(model);
                return View(model);
            }

            var result = await _service.UpdateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await NapDuLieuFormAsync(model);
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // POST: /DatPhong/Cancel/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _service.CancelAsync(id);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // Nap danh sach khach hang va phong cho bieu mau
        private async Task NapDuLieuFormAsync(DatPhongViewModel model)
        {
            model.KhachHangs = await _context.KhachHangs
                .AsNoTracking()
                .OrderBy(k => k.HoTen)
                .Select(k => new KhachHangOption
                {
                    MaKH = k.MaKH,
                    HoTen = k.HoTen,
                    SoDienThoai = k.SoDienThoai
                })
                .ToListAsync();

            model.Phongs = await _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .OrderBy(p => p.SoPhong)
                .Select(p => new PhongOption
                {
                    MaPhong = p.MaPhong,
                    SoPhong = p.SoPhong,
                    TenLoaiPhong = p.LoaiPhong != null
                        ? p.LoaiPhong.TenLoaiPhong
                        : "Chưa phân loại",
                    GiaTheoDem = p.LoaiPhong != null
                        ? p.LoaiPhong.GiaTheoDem
                        : 0,
                    TrangThai = p.TrangThai
                })
                .ToListAsync();
        }

        // GET: /DatPhong/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var datPhong = await _service.GetByIdAsync(id);
            if (datPhong == null)
                return NotFound();

            if (datPhong.TrangThai != "DaHuy")
            {
                TempData["Error"] = "Chỉ có thể xóa phiếu đã hủy.";
                return RedirectToAction(nameof(Index));
            }

            return View(datPhong);
        }

        // POST: /DatPhong/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int maDatPhong)
        {
            var result = await _service.DeleteAsync(maDatPhong);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int id, int maPhong)
        {
            var result = await _service.CheckInAsync(id, maPhong);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Json(new
            {
                success = true,
                message = result.Message
            });
        }
    }
}
