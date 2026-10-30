using HotelManagement.Data;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        // GET: /Dashboard/TaiChinh
        public async Task<IActionResult> TaiChinh()
        {
            var now = DateTime.Now;
            var today = now.Date;

            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = today.AddDays(-1 * diff);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Lấy danh sách hóa đơn đã thanh toán
            var queryPaid = _context.HoaDons
                .AsNoTracking()
                .Where(h => h.TrangThai == "DaThanhToan");

            var listHoaDon = await queryPaid.ToListAsync();
            var maDatPhongIds = listHoaDon.Select(h => h.MaDatPhong).Distinct().ToList();

            // Truy vấn lấy tổng tiền dịch vụ thực tế của từng mã đặt phòng từ bảng ChiTietDichVus
            var dichVuDict = await _context.ChiTietDichVus
                .AsNoTracking()
                .Where(cdv => cdv.LuuTru != null &&
                              cdv.LuuTru.ChiTietDatPhong != null &&
                              maDatPhongIds.Contains(cdv.LuuTru.ChiTietDatPhong.MaDatPhong))
                .Select(cdv => new {
                    MaDatPhong = cdv.LuuTru!.ChiTietDatPhong!.MaDatPhong,
                    ThanhTien = cdv.SoLuong * cdv.DonGia
                })
                .ToListAsync();

            var serviceTotalByDp = dichVuDict
                .GroupBy(x => x.MaDatPhong)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.ThanhTien));

            // Xử lý chuẩn hóa và bóc tách tiền phòng & tiền dịch vụ cho từng hóa đơn
            var processedInvoices = listHoaDon.Select(h => {
                decimal serviceAmount = h.TienDichVu > 0 ? h.TienDichVu : (serviceTotalByDp.ContainsKey(h.MaDatPhong) ? serviceTotalByDp[h.MaDatPhong] : 0m);
                decimal total = h.TongTien;
                decimal roomAmount = h.TienPhong > 0 ? h.TienPhong : Math.Max(0, total - serviceAmount);

                return new
                {
                    h.NgayLap,
                    TongTien = total,
                    TienPhong = roomAmount,
                    TienDichVu = serviceAmount
                };
            }).ToList();

            var doanhThuHomNay = processedInvoices.Where(h => h.NgayLap >= today).Sum(h => h.TongTien);
            var doanhThuTuanNay = processedInvoices.Where(h => h.NgayLap >= startOfWeek).Sum(h => h.TongTien);
            var doanhThuThangNay = processedInvoices.Where(h => h.NgayLap >= startOfMonth).Sum(h => h.TongTien);
            var tongDoanhThu = processedInvoices.Sum(h => h.TongTien);

            decimal tongTienPhong = processedInvoices.Sum(h => h.TienPhong);
            decimal tongTienDichVu = processedInvoices.Sum(h => h.TienDichVu);

            var tongCoCau = tongTienPhong + tongTienDichVu;
            double tiLePhong = tongCoCau > 0 ? (double)Math.Round(tongTienPhong / tongCoCau * 100, 1) : 100;
            double tiLeDichVu = tongCoCau > 0 ? (double)Math.Round(tongTienDichVu / tongCoCau * 100, 1) : 0;

            // 7 ngày gần nhất cho biểu đồ
            var sevenDaysAgo = today.AddDays(-6);
            var labels = new List<string>();
            var valuesDoanhThu = new List<decimal>();
            var valuesPhong = new List<decimal>();
            var valuesDichVu = new List<decimal>();

            for (int i = 0; i < 7; i++)
            {
                var currentDay = sevenDaysAgo.AddDays(i);
                labels.Add(currentDay.ToString("dd/MM"));

                var invoicesInDay = processedInvoices
                    .Where(h => h.NgayLap.Date == currentDay)
                    .ToList();

                decimal dayTotal = invoicesInDay.Sum(h => h.TongTien);
                decimal dayPhong = invoicesInDay.Sum(h => h.TienPhong);
                decimal dayDichVu = invoicesInDay.Sum(h => h.TienDichVu);

                valuesDoanhThu.Add(dayTotal);
                valuesPhong.Add(dayPhong);
                valuesDichVu.Add(dayDichVu);
            }

            var model = new FinancialDashboardViewModel
            {
                DoanhThuHomNay = doanhThuHomNay,
                DoanhThuTuanNay = doanhThuTuanNay,
                DoanhThuThangNay = doanhThuThangNay,
                TongDoanhThu = tongDoanhThu,
                DoanhThuPhong = tongTienPhong,
                DoanhThuDichVu = tongTienDichVu,
                TiLeDoanhThuPhong = tiLePhong,
                TiLeDoanhThuDichVu = tiLeDichVu,
                Labels7Ngay = labels,
                DoanhThu7Ngay = valuesDoanhThu,
                DoanhThuPhong7Ngay = valuesPhong,
                DoanhThuDichVu7Ngay = valuesDichVu,
                TongHoaDonDaThu = processedInvoices.Count
            };

            return View("TaiChinh", model);
        }

    }
}
