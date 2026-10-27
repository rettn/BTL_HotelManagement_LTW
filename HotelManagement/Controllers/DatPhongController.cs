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

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Json(new { success = true, message = result.Message });
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
        
        // POST: /DatPhong/CheckOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int id, int maPhong)
        {
            var result = await _service.CheckOutAsync(id, maPhong);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Json(new { success = true, message = result.Message });
        }

        // POST: /DatPhong/DonDepXong
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DonDepXong(int maPhong)
        {
            var phong = await _context.Phongs
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

            if (phong == null)
                return BadRequest(new { success = false, message = "Không tìm thấy phòng." });

            var trangThai = phong.TrangThai?.Trim();
            if (trangThai != "DangDon" && trangThai != "Đang dọn dẹp")
                return BadRequest(new { success = false, message = "Phòng không ở trạng thái đang dọn dẹp." });

            phong.TrangThai = "Trong";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Phòng đã dọn dẹp xong và sẵn sàng đón khách." });
        }

        // GET: /DatPhong/DichVuLuuTru?maPhong=3
        [HttpGet]
        public async Task<IActionResult> DichVuLuuTru(int maPhong)
        {
            var luuTru = await _context.LuuTrus
                .AsNoTracking()
                .Include(l => l.ChiTietDatPhong)
                    .ThenInclude(c => c!.Phong)
                .Include(l => l.ChiTietDichVus)
                    .ThenInclude(d => d.DichVu)
                .Where(l =>
                    l.ChiTietDatPhong != null &&
                    l.ChiTietDatPhong.MaPhong == maPhong &&
                    l.ThoiGianNhanPhong != null &&
                    l.ThoiGianTraPhong == null)
                .OrderByDescending(l => l.ThoiGianNhanPhong)
                .FirstOrDefaultAsync();

            if (luuTru == null)
            {
                return Content(
                    "<div class='type-empty'><p>Phòng này hiện không có khách đang lưu trú.</p></div>",
                    "text/html");
            }

            ViewBag.DanhSachDichVu = await _context.DichVus
                .AsNoTracking()
                .Where(d => d.TrangThai)
                .OrderBy(d => d.TenDichVu)
                .ToListAsync();

            return PartialView("_DichVuLuuTru", luuTru);
        }

        // POST: /DatPhong/ThemDichVu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemDichVu(int maLuuTru, int maDichVu, int soLuong)
        {
            if (soLuong < 1 || soLuong > 100)
                return BadRequest(new { success = false, message = "Số lượng phải từ 1 đến 100." });

            var luuTru = await _context.LuuTrus
                .FirstOrDefaultAsync(l => l.MaLuuTru == maLuuTru);

            if (luuTru == null || luuTru.ThoiGianTraPhong != null)
                return BadRequest(new { success = false, message = "Phòng không còn khách đang lưu trú." });

            var dichVu = await _context.DichVus
                .FirstOrDefaultAsync(d => d.Id == maDichVu && d.TrangThai);

            if (dichVu == null)
                return BadRequest(new { success = false, message = "Dịch vụ không tồn tại hoặc đã ngừng cung cấp." });

            _context.ChiTietDichVus.Add(new ChiTietDichVu
            {
                MaLuuTru = luuTru.MaLuuTru,
                MaDichVu = dichVu.Id,
                SoLuong = soLuong,
                DonGia = dichVu.Gia,
                ThoiGianSuDung = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã thêm dịch vụ." });
        }
    }
}
