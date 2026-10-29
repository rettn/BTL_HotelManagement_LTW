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

            // Lấy danh sách hóa đơn đã thanh toán kèm thông tin đặt phòng để tính toán dự phòng
            var queryPaid = _context.HoaDons
                .AsNoTracking()
                .Include(h => h.DatPhong)
                    .ThenInclude(dp => dp!.ChiTietDatPhongs)
                .Where(h => h.TrangThai == "DaThanhToan");

            var listHoaDon = await queryPaid.ToListAsync();

            var doanhThuHomNay = listHoaDon.Where(h => h.NgayLap >= today).Sum(h => h.TongTien);
            var doanhThuTuanNay = listHoaDon.Where(h => h.NgayLap >= startOfWeek).Sum(h => h.TongTien);
            var doanhThuThangNay = listHoaDon.Where(h => h.NgayLap >= startOfMonth).Sum(h => h.TongTien);
            var tongDoanhThu = listHoaDon.Sum(h => h.TongTien);

            // Xử lý chuẩn hóa Tiền phòng & Tiền dịch vụ (nếu bảng HoaDons lưu 0đ thì lấy Tổng tiền làm tiền phòng tạm thời)
            decimal tongTienPhong = listHoaDon.Sum(h => h.TienPhong > 0 ? h.TienPhong : h.TongTien);
            decimal tongTienDichVu = listHoaDon.Sum(h => h.TienDichVu);

            var tongCoCau = tongTienPhong + tongTienDichVu;
            if (tongCoCau == 0 && tongDoanhThu > 0)
            {
                tongTienPhong = tongDoanhThu;
            }

            tongCoCau = tongTienPhong + tongTienDichVu;
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

                var invoicesInDay = listHoaDon
                    .Where(h => h.NgayLap.Date == currentDay)
                    .ToList();

                decimal dayTotal = invoicesInDay.Sum(h => h.TongTien);
                decimal dayPhong = invoicesInDay.Sum(h => h.TienPhong > 0 ? h.TienPhong : h.TongTien);
                if (dayPhong == 0 && dayTotal > 0) dayPhong = dayTotal;

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
                TongHoaDonDaThu = listHoaDon.Count
            };

            return View("TaiChinh", model);
        }
    }
}
