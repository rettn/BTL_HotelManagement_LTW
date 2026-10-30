using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class DashBoardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashBoardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /DashBoard/Index (Trang Tổng quan)
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            int tongSoPhong = await _context.Phongs.CountAsync();
            int phongDangSuDung = await _context.Phongs
                .CountAsync(p => p.TrangThai != null &&
                    (p.TrangThai.Contains("DangSuDung") || p.TrangThai.Contains("Đang sử dụng") || p.TrangThai.Contains("sử dụng")));

            int phieuChoXacNhan = await _context.DatPhongs
                .CountAsync(dp => dp.TrangThai == "ChoXacNhan");

            decimal doanhThuHomNay = await _context.ChiTietDatPhongs
                .Where(c => c.DatPhong != null && c.DatPhong.TrangThai != "DaHuy" && c.NgayNhan <= today && c.NgayTra >= today)
                .SumAsync(c => (decimal?)c.DonGia) ?? 0m;

            int phongTrong = await _context.Phongs.CountAsync(p => p.TrangThai == "Trong" || p.TrangThai == "Trống");
            int phongDangDon = await _context.Phongs.CountAsync(p => p.TrangThai == "DangDon" || p.TrangThai == "Đang dọn dẹp");
            int phongDaDat = await _context.Phongs.CountAsync(p => p.TrangThai == "DaDat" || p.TrangThai == "Đã đặt");

            var dayOfWeek = (int)today.DayOfWeek;
            var startOfWeek = today.AddDays(-((dayOfWeek == 0 ? 7 : dayOfWeek) - 1));
            var doanhThuTuan = new List<decimal>();

            for (int i = 0; i < 7; i++)
            {
                var curDate = startOfWeek.AddDays(i);
                var val = await _context.ChiTietDatPhongs
                    .Where(c => c.DatPhong != null && c.DatPhong.TrangThai != "DaHuy" && c.NgayNhan <= curDate && c.NgayTra >= curDate)
                    .SumAsync(c => (decimal?)c.DonGia) ?? 0m;
                doanhThuTuan.Add(val);
            }

            var datPhongGanDay = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .OrderByDescending(dp => dp.NgayDat)
                .Take(5)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TongSoPhong = tongSoPhong,
                PhongDangSuDung = phongDangSuDung,
                PhieuChoXacNhan = phieuChoXacNhan,
                DoanhThuHomNay = doanhThuHomNay,
                SoPhongTrong = phongTrong,
                SoPhongDangDon = phongDangDon,
                SoPhongDangSuDung = phongDangSuDung,
                SoPhongDaDat = phongDaDat,
                DoanhThuTuan = doanhThuTuan,
                DatPhongGanDay = datPhongGanDay
            };

            return View(viewModel);
        }

        // GET: /DashBoard/TaiChinh (Trang Báo cáo tài chính)
        public async Task<IActionResult> TaiChinh()
        {
            var today = DateTime.Today;

            // Doanh thu phòng & dịch vụ tổng tích lũy
            decimal doanhThuPhong = await _context.ChiTietDatPhongs
                .Where(c => c.DatPhong != null && c.DatPhong.TrangThai != "DaHuy")
                .SumAsync(c => (decimal?)c.DonGia) ?? 0m;

            decimal doanhThuDichVu = await _context.ChiTietDichVus
                .SumAsync(c => (decimal?)(c.SoLuong * c.DonGia)) ?? 0m;

            decimal tongDoanhThu = doanhThuPhong + doanhThuDichVu;

            // Tỷ lệ %
            double tiLePhong = tongDoanhThu > 0 ? Math.Round((double)(doanhThuPhong / tongDoanhThu * 100), 1) : 0;
            double tiLeDichVu = tongDoanhThu > 0 ? Math.Round(100 - tiLePhong, 1) : 0;

            // Doanh thu hôm nay
            decimal dtPhongHomNay = await _context.ChiTietDatPhongs
                .Where(c => c.DatPhong != null && c.DatPhong.TrangThai != "DaHuy" && c.NgayNhan <= today && c.NgayTra >= today)
                .SumAsync(c => (decimal?)c.DonGia) ?? 0m;
            decimal dtDichVuHomNay = await _context.ChiTietDichVus
                .Where(c => c.ThoiGianSuDung.Date == today)
                .SumAsync(c => (decimal?)(c.SoLuong * c.DonGia)) ?? 0m;
            decimal dtHomNay = dtPhongHomNay + dtDichVuHomNay;

            // Doanh thu tuần này & tháng này
            var startOfWeek = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1));
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            decimal dtTuanNay = (await _context.ChiTietDatPhongs
                .Where(c => c.DatPhong != null && c.DatPhong.TrangThai != "DaHuy" && c.NgayNhan >= startOfWeek)
                .SumAsync(c => (decimal?)c.DonGia) ?? 0m) +
                (await _context.ChiTietDichVus
                .Where(c => c.ThoiGianSuDung >= startOfWeek)
                .SumAsync(c => (decimal?)(c.SoLuong * c.DonGia)) ?? 0m);

            decimal dtThangNay = (await _context.ChiTietDatPhongs
                .Where(c => c.DatPhong != null && c.DatPhong.TrangThai != "DaHuy" && c.NgayNhan >= startOfMonth)
                .SumAsync(c => (decimal?)c.DonGia) ?? 0m) +
                (await _context.ChiTietDichVus
                .Where(c => c.ThoiGianSuDung >= startOfMonth)
                .SumAsync(c => (decimal?)(c.SoLuong * c.DonGia)) ?? 0m);

            // Mảng 7 ngày gần nhất
            var labels7Ngay = new List<string>();
            var dtPhong7Ngay = new List<decimal>();
            var dtDichVu7Ngay = new List<decimal>();

            for (int i = 6; i >= 0; i--)
            {
                var d = today.AddDays(-i);
                labels7Ngay.Add(d.ToString("dd/MM"));

                var pVal = await _context.ChiTietDatPhongs
                    .Where(c => c.DatPhong != null && c.DatPhong.TrangThai != "DaHuy" && c.NgayNhan <= d && c.NgayTra >= d)
                    .SumAsync(c => (decimal?)c.DonGia) ?? 0m;

                var dvVal = await _context.ChiTietDichVus
                    .Where(c => c.ThoiGianSuDung.Date == d)
                    .SumAsync(c => (decimal?)(c.SoLuong * c.DonGia)) ?? 0m;

                dtPhong7Ngay.Add(pVal);
                dtDichVu7Ngay.Add(dvVal);
            }

            var vm = new FinancialDashboardViewModel
            {
                DoanhThuHomNay = dtHomNay,
                DoanhThuTuanNay = dtTuanNay,
                DoanhThuThangNay = dtThangNay,
                TongDoanhThu = tongDoanhThu,
                DoanhThuPhong = doanhThuPhong,
                DoanhThuDichVu = doanhThuDichVu,
                TiLeDoanhThuPhong = tiLePhong,
                TiLeDoanhThuDichVu = tiLeDichVu,
                Labels7Ngay = labels7Ngay,
                DoanhThuPhong7Ngay = dtPhong7Ngay,
                DoanhThuDichVu7Ngay = dtDichVu7Ngay
            };

            return View(vm);
        }
    }
}