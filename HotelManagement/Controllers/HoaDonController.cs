using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class HoaDonController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HoaDonController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /HoaDon
        public async Task<IActionResult> Index(
            string? tuKhoa,
            string? trangThai,
            DateTime? tuNgay,
            DateTime? denNgay,
            int? maDatPhong,
            int page = 1,
            int pageSize = 10)
        {
            ViewBag.MaDatPhong = maDatPhong;
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.HoaDons
                .Include(h => h.DatPhong)
                    .ThenInclude(dp => dp!.KhachHang)
                .Include(h => h.DatPhong)
                    .ThenInclude(dp => dp!.ChiTietDatPhongs)
                        .ThenInclude(ct => ct.Phong)
                .Include(h => h.ThanhToans)
                .AsNoTracking()
                .AsQueryable();

            if (maDatPhong.HasValue)
                query = query.Where(h => h.MaDatPhong == maDatPhong.Value);

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var keyword = tuKhoa.Trim().ToLower();
                query = query.Where(h =>
                    h.MaHD.ToString().Contains(keyword) ||
                    (h.DatPhong != null && h.DatPhong.KhachHang != null &&
                        (h.DatPhong.KhachHang.HoTen.ToLower().Contains(keyword) ||
                         (h.DatPhong.KhachHang.SoDienThoai != null && h.DatPhong.KhachHang.SoDienThoai.Contains(keyword))))
                );
            }

            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "TatCa")
            {
                query = query.Where(h => h.TrangThai == trangThai);
            }

            if (tuNgay.HasValue)
            {
                var fromDate = tuNgay.Value.Date;
                query = query.Where(h => h.NgayLap >= fromDate);
            }

            if (denNgay.HasValue)
            {
                var toDate = denNgay.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(h => h.NgayLap <= toDate);
            }

            var totalItems = await query.CountAsync();
            var tongTienDaThu = await query
                .Where(h => h.TrangThai == "DaThanhToan")
                .SumAsync(h => (decimal?)h.TongTien) ?? 0m;

            var hoaDons = await query
                .OrderByDescending(h => h.NgayLap)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new HoaDonItemViewModel
                {
                    MaHD = h.MaHD,
                    MaDatPhong = h.MaDatPhong,
                    TenKhachHang = h.DatPhong != null && h.DatPhong.KhachHang != null
                        ? h.DatPhong.KhachHang.HoTen
                        : "Khách lẻ",
                    SoDienThoai = h.DatPhong != null && h.DatPhong.KhachHang != null
                        ? (h.DatPhong.KhachHang.SoDienThoai ?? "")
                        : "",
                    DanhSachPhong = h.ChiTietHoaDons.Any(c => c.LoaiKhoanThu == "Phong")
                        ? string.Join(", ", h.ChiTietHoaDons.Where(c => c.LoaiKhoanThu == "Phong").Select(c => c.TenKhoanThu))
                        : h.DatPhong != null
                            ? string.Join(", ", h.DatPhong.ChiTietDatPhongs.Select(c => c.Phong != null ? c.Phong.SoPhong : ""))
                            : "",
                    NgayLap = h.NgayLap,
                    TienPhong = h.TienPhong,
                    TienDichVu = h.TienDichVu,
                    GiamGia = h.GiamGia,
                    TongTien = h.TongTien,
                    TrangThai = h.DatPhong != null && h.DatPhong.TrangThai == "DaHuy" ? "DaHuy" : h.TrangThai,
                    PhuongThucThanhToan = h.ThanhToans.OrderByDescending(t => t.NgayThanhToan).Select(t => t.PhuongThuc).FirstOrDefault() ?? "Chưa rõ"
                })
                .ToListAsync();

            var viewModel = new HoaDonIndexViewModel
            {
                TuKhoa = tuKhoa,
                TrangThai = trangThai,
                TuNgay = tuNgay,
                DenNgay = denNgay,
                DanhSachHoaDon = hoaDons,
                PageIndex = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TongSoHoaDon = totalItems,
                TongTienDaThu = tongTienDaThu
            };

            return View(viewModel);
        }

        // GET: /HoaDon/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.DatPhong)
                .Include(h => h.ThanhToans)
                .Include(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(h => h.MaHD == id);

            if (hoaDon == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = await GetHoaDonCheckoutViewModelAsync(hoaDon.MaDatPhong);
            if (viewModel == null)
            {
                return NotFound();
            }

            var phongTrenHoaDon = hoaDon.ChiTietHoaDons.Where(c => c.LoaiKhoanThu == "Phong").ToList();
            if (phongTrenHoaDon.Count > 0)
            {
                viewModel.DanhSachPhong = phongTrenHoaDon.Select(c => new PhongHoaDonItemViewModel
                {
                    SoPhong = c.TenKhoanThu.Replace("Tiền phòng", "").Trim(),
                    TenLoaiPhong = viewModel.TenLoaiPhong,
                    GiaTheoDem = c.DonGia,
                    SoDem = c.SoLuong,
                    ThanhTien = c.ThanhTien
                }).ToList();
                viewModel.SoPhong = string.Join(", ", viewModel.DanhSachPhong.Select(p => p.SoPhong));
                viewModel.TienPhong = hoaDon.TienPhong;
                viewModel.DanhSachDichVu = hoaDon.ChiTietHoaDons.Where(c => c.LoaiKhoanThu == "DichVu")
                    .Select(c => new ChiTietDichVuItemViewModel
                    {
                        TenDichVu = c.TenKhoanThu,
                        SoLuong = c.SoLuong,
                        DonGia = c.DonGia,
                        ThanhTien = c.ThanhTien
                    }).ToList();
                viewModel.TongTienDichVu = hoaDon.TienDichVu;
                viewModel.TongThanhToan = hoaDon.TongTien;
                viewModel.TienThue = hoaDon.TongTien - hoaDon.TienPhong - hoaDon.TienDichVu;
                viewModel.TienCoc = 0;
                viewModel.SoTienConLai = hoaDon.ThanhToans.Where(t => t.TrangThai == "ThanhCong").Sum(t => t.SoTien);
            }

            // Đồng bộ trạng thái thực tế từ DatPhong sang ViewBag để hiển thị đúng ở View
            string trangThaiHienThi = hoaDon.TrangThai;
            if (hoaDon.DatPhong != null)
            {
                if (hoaDon.DatPhong.TrangThai == "DaHuy")
                {
                    trangThaiHienThi = "DaHuy";
                }
                else if (hoaDon.DatPhong.TrangThai == "DaCoc")
                {
                    trangThaiHienThi = "DaCoc";
                }
            }

            ViewBag.MaHoaDon = $"HD{hoaDon.MaHD:D6}";
            ViewBag.TrangThai = trangThaiHienThi;
            ViewBag.NgayLap = hoaDon.NgayLap;
            ViewBag.PhuongThucThanhToan = hoaDon.ThanhToans.OrderByDescending(t => t.NgayThanhToan).Select(t => t.PhuongThuc).FirstOrDefault() ?? "TienMat";

            return View(viewModel);
        }

        // HÀM BỔ TRỢ TÍNH TOÁN DỮ LIỆU HÓA ĐƠN CHUẨN XÁC
        private async Task<HoaDonCheckoutViewModel?> GetHoaDonCheckoutViewModelAsync(int maDatPhong)
        {
            var datPhong = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p.LoaiPhong)
                .FirstOrDefaultAsync(dp => dp.MaDatPhong == maDatPhong);

            if (datPhong == null) return null;

            var chiTietPhong = datPhong.ChiTietDatPhongs.FirstOrDefault();
            var phong = chiTietPhong?.Phong;
            var loaiPhong = phong?.LoaiPhong;

            DateTime ngayNhan = datPhong.NgayNhanThucTe != default ? datPhong.NgayNhanThucTe : datPhong.NgayNhanDuKien;
            DateTime ngayTra = datPhong.NgayTraThucTe != default ? datPhong.NgayTraThucTe : DateTime.Now;

            int soDem = (int)(ngayTra.Date - ngayNhan.Date).TotalDays;
            if (soDem <= 0) soDem = 1;

            decimal giaTheoDem = chiTietPhong?.DonGia ?? loaiPhong?.GiaTheoDem ?? 0;
            decimal tienPhong = giaTheoDem * soDem; // Đã fix lỗi ngắt dòng cú pháp

            var dsDichVu = await _context.ChiTietDichVus
                .AsNoTracking()
                .Include(cdv => cdv.DichVu)
                .Where(cdv => _context.LuuTrus.Any(l => l.MaLuuTru == cdv.MaLuuTru
                                                     && l.ChiTietDatPhong != null
                                                     && l.ChiTietDatPhong.MaDatPhong == maDatPhong))
                .Select(cdv => new ChiTietDichVuItemViewModel
                {
                    TenDichVu = cdv.DichVu != null ? cdv.DichVu.TenDichVu : "Dịch vụ",
                    SoLuong = cdv.SoLuong,
                    DonGia = cdv.DonGia,
                    ThanhTien = cdv.SoLuong * cdv.DonGia
                })
                .ToListAsync();

            decimal tongTienDichVu = dsDichVu.Sum(x => x.ThanhTien);
            decimal tiLeThuePercent = 10;
            decimal tienThue = (tienPhong + tongTienDichVu) * (tiLeThuePercent / 100m);
            decimal tongThanhToan = tienPhong + tongTienDichVu + tienThue;

            return new HoaDonCheckoutViewModel
            {
                MaDatPhong = datPhong.MaDatPhong,
                SoPhong = phong?.SoPhong ?? "---",
                TenKhachHang = datPhong.KhachHang?.HoTen ?? "Khách vãng lai",
                SoDienThoai = datPhong.KhachHang?.SoDienThoai ?? "",
                TenLoaiPhong = loaiPhong?.TenLoaiPhong ?? "Tiêu chuẩn",
                NgayNhan = ngayNhan,
                NgayTraThucTe = ngayTra,
                SoDem = soDem,
                GiaTheoDem = giaTheoDem,
                TienPhong = tienPhong,
                DanhSachDichVu = dsDichVu,
                TongTienDichVu = tongTienDichVu,
                TiLeThuePercent = tiLeThuePercent,
                TienThue = tienThue,
                TongThanhToan = tongThanhToan
            };
        }
    }
}
