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
            // Thời gian nhận phòng thực tế của các phòng đang có khách (chỉ đọc, không đổi nghiệp vụ)
            var luuTruDangO = await _context.LuuTrus
                .AsNoTracking()
                .Where(l => l.ChiTietDatPhong != null
                         && l.ThoiGianNhanPhong != null
                         && l.ThoiGianTraPhong == null)
                .Select(l => new { l.ChiTietDatPhong!.MaPhong, ThoiGian = l.ThoiGianNhanPhong!.Value })
                .ToListAsync();

            ViewBag.ThoiGianNhan = luuTruDangO
                .GroupBy(x => x.MaPhong)
                .ToDictionary(g => g.Key, g => g.Max(x => x.ThoiGian));
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

            var tt = phong.TrangThai?.Trim() ?? "";
            bool laTrong = tt == "Trong" || tt == "Trống";
            bool dangDon = tt == "DangDon" || tt == "Đang dọn dẹp";
            bool dangSuDung = tt == "DangSuDung" || tt == "Đang sử dụng";
            bool daDat = tt == "DaDat" || tt == "Đã đặt";

            ChiTietDatPhong? chiTiet = null;

            if (dangSuDung)
            {
                // Phòng đang sử dụng: Lấy phiếu mà khách ĐANG lưu trú thực tế (chưa trả phòng)
                chiTiet = await _context.ChiTietDatPhongs
                    .AsNoTracking()
                    .Include(c => c.DatPhong)
                        .ThenInclude(d => d!.KhachHang)
                    .Include(c => c.DanhSachLuuTru)
                    .Where(c => c.MaPhong == id &&
                                c.DatPhong != null &&
                                c.DatPhong.TrangThai != "DaHuy" &&
                                c.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null && l.ThoiGianTraPhong == null))
                    .OrderByDescending(c => c.DanhSachLuuTru
                        .Where(l => l.ThoiGianNhanPhong != null && l.ThoiGianTraPhong == null)
                        .Max(l => l.ThoiGianNhanPhong))
                    .FirstOrDefaultAsync();
            }
            else if (daDat)
            {
                // Phòng đã đặt trước: Khách chưa nhận phòng, lấy phiếu đặt phòng sắp tới gần nhất
                chiTiet = await _context.ChiTietDatPhongs
                    .AsNoTracking()
                    .Include(c => c.DatPhong)
                        .ThenInclude(d => d!.KhachHang)
                    .Include(c => c.DanhSachLuuTru)
                    .Where(c => c.MaPhong == id &&
                                c.DatPhong != null &&
                                c.DatPhong.TrangThai != "DaHuy" &&
                                c.DatPhong.TrangThai != "DaThanhToan" &&
                                c.DatPhong.TrangThai != "DaHoanThanh" &&
                                !c.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null))
                    .OrderBy(c => c.NgayNhan)
                    .FirstOrDefaultAsync();
            }
            else
            {
                // Phòng trống hoặc đang dọn dẹp: Khách cũ đã trả phòng rồi.
                // TUYỆT ĐỐI KHÔNG lấy phiếu của khách cũ đã trả phòng (đã có ThoiGianTraPhong hoặc đã từng nhận phòng).
                // Chỉ lấy phiếu NẾU có một lịch đặt phòng MỚI trong tương lai chưa nhận phòng.
                chiTiet = await _context.ChiTietDatPhongs
                    .AsNoTracking()
                    .Include(c => c.DatPhong)
                        .ThenInclude(d => d!.KhachHang)
                    .Include(c => c.DanhSachLuuTru)
                    .Where(c => c.MaPhong == id &&
                                c.DatPhong != null &&
                                c.DatPhong.TrangThai != "DaHuy" &&
                                c.DatPhong.TrangThai != "DaThanhToan" &&
                                c.DatPhong.TrangThai != "DaHoanThanh" &&
                                c.NgayNhan.Date >= DateTime.Today &&
                                !c.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null))
                    .OrderBy(c => c.NgayNhan)
                    .FirstOrDefaultAsync();
            }

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