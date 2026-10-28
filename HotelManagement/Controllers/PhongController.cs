using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class PhongController : Controller
    {
        private readonly PhongService _service;
        private readonly ApplicationDbContext _context;

        public PhongController(
            PhongService service,
            ApplicationDbContext context)
        {
            _service = service;
            _context = context;
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
            var phong = await _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(p => p.MaPhong == id);

            if (phong == null)
                return NotFound();

            // Lay cac chi tiet dat phong cua phong nay,
            // khong lay cac phieu da huy.
            var danhSachChiTiet = await _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(c => c.DatPhong)
                    .ThenInclude(d => d!.KhachHang)
                .Include(c => c.DanhSachLuuTru)
                .Where(c =>
                    c.MaPhong == id &&
                    c.DatPhong != null &&
                    c.DatPhong.TrangThai != "DaHuy" &&
                    c.DatPhong.TrangThai != "DaThanhToan" &&
                    c.DatPhong.TrangThai != "DaHoanThanh")
                .ToListAsync();

            var hienTai = DateTime.Now;

            // Uu tien phieu dang co khach luu tru.
            // Neu khong co, chon phieu co ngay tra trong tuong lai
            // gan nhat.
            var chiTiet = danhSachChiTiet
                .Where(c => c.DanhSachLuuTru.Any(l =>
                    l.ThoiGianNhanPhong != null &&
                    l.ThoiGianTraPhong == null))
                .OrderByDescending(c => c.DanhSachLuuTru
                    .Where(l => l.ThoiGianNhanPhong != null &&
                                l.ThoiGianTraPhong == null)
                    .Max(l => l.ThoiGianNhanPhong))
                .FirstOrDefault();

            chiTiet ??= danhSachChiTiet
                .Where(c => c.NgayTra > hienTai.Date)
                .OrderBy(c => c.NgayNhan)
                .FirstOrDefault();

            // Neu khong co lich tuong lai, van hien thi lich gan nhat
            // de ho tro doi chieu trang thai phong.
            chiTiet ??= danhSachChiTiet
                .OrderByDescending(c => c.NgayNhan)
                .FirstOrDefault();

            ViewBag.ChiTietDatPhong = chiTiet;
            ViewBag.DatPhong = chiTiet?.DatPhong;
            ViewBag.KhachHang = chiTiet?.DatPhong?.KhachHang;

            var luuTru = chiTiet?.DanhSachLuuTru?
                .Where(l => l.ThoiGianNhanPhong != null &&
                            l.ThoiGianTraPhong == null)
                .OrderByDescending(l => l.ThoiGianNhanPhong)
                .FirstOrDefault();

            ViewBag.LuuTru = luuTru;

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