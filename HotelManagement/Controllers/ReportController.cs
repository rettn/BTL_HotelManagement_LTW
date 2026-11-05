using HotelManagement.Data;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Report hoặc /Report/Index
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            return await Dashboard(startDate, endDate);
        }

        // GET: /Report/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard(DateTime? startDate, DateTime? endDate)
        {
            var now = DateTime.Now;
            var today = DateTime.Today;

            // Mặc định lọc 30 ngày gần nhất nếu chưa chọn mốc thời gian
            startDate ??= today.AddDays(-30);
            endDate ??= today;

            var fromDate = startDate.Value.Date;
            var toDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

            var viewModel = new ReportDashboardViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Tập hóa đơn đã thanh toán
            var queryPaidInvoices = _context.HoaDons
                .AsNoTracking()
                .Where(h => h.TrangThai == "DaThanhToan");

            // =========================================================================
            // A. TÍNH TOÁN CÁC CHỈ SỐ DOANH THU & 7 NGÀY GẦN NHẤT (CHO BIỂU ĐỒ CỘT VÀ TRÒN)
            // =========================================================================
            // 1. Doanh thu hôm nay
            viewModel.DoanhThuHomNay = await queryPaidInvoices
                .Where(h => h.NgayLap >= today)
                .SumAsync(h => (decimal?)h.TongTien) ?? 0m;

            // 2. Doanh thu tuần này (tính từ Thứ Hai)
            var dayOfWeek = (int)today.DayOfWeek;
            var startOfWeek = today.AddDays(-((dayOfWeek == 0 ? 7 : dayOfWeek) - 1));
            viewModel.DoanhThuTuanNay = await queryPaidInvoices
                .Where(h => h.NgayLap >= startOfWeek)
                .SumAsync(h => (decimal?)h.TongTien) ?? 0m;

            // 3. Doanh thu tháng này
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            viewModel.DoanhThuThangNay = await queryPaidInvoices
                .Where(h => h.NgayLap >= startOfMonth)
                .SumAsync(h => (decimal?)h.TongTien) ?? 0m;

            // 4. Cơ cấu doanh thu trong kỳ lọc (Tiền phòng vs Tiền dịch vụ cho Biểu đồ tròn)
            var hoaDonTrongKy = await queryPaidInvoices
                .Where(h => h.NgayLap >= fromDate && h.NgayLap <= toDate)
                .Select(h => new { h.TienPhong, h.TienDichVu, h.TongTien, h.NgayLap })
                .ToListAsync();

            decimal dtPhongTrongKy = hoaDonTrongKy.Sum(h => h.TienPhong);
            decimal dtDichVuTrongKy = hoaDonTrongKy.Sum(h => h.TienDichVu);
            decimal tongDoanhThuTrongKy = dtPhongTrongKy + dtDichVuTrongKy;

            viewModel.DoanhThuPhong = dtPhongTrongKy;
            viewModel.DoanhThuDichVu = dtDichVuTrongKy;
            viewModel.TongDoanhThuKyNay = hoaDonTrongKy.Sum(h => h.TongTien);
            viewModel.TiLeDoanhThuPhong = tongDoanhThuTrongKy > 0 ? Math.Round((double)(dtPhongTrongKy / tongDoanhThuTrongKy * 100), 1) : 0;
            viewModel.TiLeDoanhThuDichVu = tongDoanhThuTrongKy > 0 ? Math.Round(100 - viewModel.TiLeDoanhThuPhong, 1) : 0;

            // 5. Biến động doanh thu 7 ngày gần nhất (cho Biểu đồ cột / đường)
            var sevenDaysAgo = today.AddDays(-6);
            var hoaDon7Ngay = await queryPaidInvoices
                .Where(h => h.NgayLap >= sevenDaysAgo)
                .Select(h => new { h.NgayLap, h.TongTien, h.TienPhong, h.TienDichVu })
                .ToListAsync();

            for (int i = 0; i < 7; i++)
            {
                var curDay = sevenDaysAgo.AddDays(i);
                viewModel.Labels7Ngay.Add(curDay.ToString("dd/MM"));

                var inDay = hoaDon7Ngay.Where(h => h.NgayLap.Date == curDay).ToList();
                decimal pVal = inDay.Sum(h => h.TienPhong);
                decimal dvVal = inDay.Sum(h => h.TienDichVu);

                viewModel.DoanhThuPhong7Ngay.Add(pVal);
                viewModel.DoanhThuDichVu7Ngay.Add(dvVal);
                viewModel.DoanhThu7Ngay.Add(pVal + dvVal);
            }

            // =========================================================================
            // BÁO CÁO 1: DOANH THU THEO HẠNG PHÒNG (LOẠI PHÒNG)
            // =========================================================================
            var danhSachLoaiPhong = await _context.LoaiPhongs.AsNoTracking().ToListAsync();

            var chiTietDatPhongsDaThu = await _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                .Include(ct => ct.DatPhong)
                    .ThenInclude(dp => dp!.HoaDons)
                .Where(ct => ct.DatPhong != null &&
                             ct.DatPhong.HoaDons.Any(h => h.TrangThai == "DaThanhToan"
                                                       && h.NgayLap >= fromDate
                                                       && h.NgayLap <= toDate))
                .ToListAsync();

            var listDoanhThuHangPhong = new List<RoomCategoryRevenueItem>();

            foreach (var lp in danhSachLoaiPhong)
            {
                var dsChiTietCuaLoai = chiTietDatPhongsDaThu
                    .Where(ct => ct.Phong != null && ct.Phong.MaLoaiPhong == lp.MaLoaiPhong)
                    .ToList();

                int soLuotDat = dsChiTietCuaLoai.Select(ct => ct.MaDatPhong).Distinct().Count();

                int tongSoDem = dsChiTietCuaLoai.Sum(ct =>
                {
                    var ngayTra = ct.NgayTra > ct.NgayNhan ? ct.NgayTra : ct.NgayNhan.AddDays(1);
                    return Math.Max(1, (int)(ngayTra.Date - ct.NgayNhan.Date).TotalDays);
                });

                decimal doanhThu = dsChiTietCuaLoai.Sum(ct =>
                {
                    var ngayTra = ct.NgayTra > ct.NgayNhan ? ct.NgayTra : ct.NgayNhan.AddDays(1);
                    int soDem = Math.Max(1, (int)(ngayTra.Date - ct.NgayNhan.Date).TotalDays);
                    decimal donGia = ct.DonGia > 0 ? ct.DonGia : lp.GiaTheoDem;
                    return donGia * soDem;
                });

                listDoanhThuHangPhong.Add(new RoomCategoryRevenueItem
                {
                    MaLoaiPhong = lp.MaLoaiPhong,
                    TenLoaiPhong = lp.TenLoaiPhong,
                    GiaTheoDemHienTai = lp.GiaTheoDem,
                    SoLuotDat = soLuotDat,
                    TongSoDemThue = tongSoDem,
                    DoanhThu = doanhThu
                });
            }

            decimal tongDoanhThuPhong = listDoanhThuHangPhong.Sum(x => x.DoanhThu);
            foreach (var item in listDoanhThuHangPhong)
            {
                item.TiLePhanTram = tongDoanhThuPhong > 0
                    ? Math.Round((double)(item.DoanhThu / tongDoanhThuPhong * 100), 1)
                    : 0;
            }

            viewModel.DoanhThuTheoHangPhong = listDoanhThuHangPhong
                .OrderByDescending(x => x.DoanhThu)
                .ToList();

            // =========================================================================
            // BÁO CÁO 2: TOP KHÁCH HÀNG THÂN THIẾT CHI TIÊU NHIỀU NHẤT
            // =========================================================================
            var topKhachHangsData = await _context.KhachHangs
                .AsNoTracking()
                .Include(kh => kh.DanhSachDatPhong)
                    .ThenInclude(dp => dp.HoaDons)
                .Select(kh => new
                {
                    kh.MaKH,
                    kh.HoTen,
                    kh.SoDienThoai,
                    kh.Email,
                    DonDatTrongKy = kh.DanhSachDatPhong
                        .Where(dp => dp.NgayDat >= fromDate && dp.NgayDat <= toDate)
                        .ToList(),
                    TongChiTieu = kh.DanhSachDatPhong
                        .SelectMany(dp => dp.HoaDons)
                        .Where(h => h.TrangThai == "DaThanhToan"
                                 && h.NgayLap >= fromDate
                                 && h.NgayLap <= toDate)
                        .Sum(h => (decimal?)h.TongTien) ?? 0m,
                    NgayDatGanNhat = kh.DanhSachDatPhong
                        .Where(dp => dp.NgayDat >= fromDate && dp.NgayDat <= toDate)
                        .OrderByDescending(dp => dp.NgayDat)
                        .Select(dp => (DateTime?)dp.NgayDat)
                        .FirstOrDefault()
                })
                .Where(x => x.TongChiTieu > 0 || x.DonDatTrongKy.Any())
                .OrderByDescending(x => x.TongChiTieu)
                .ThenByDescending(x => x.DonDatTrongKy.Count)
                .Take(10)
                .ToListAsync();

            viewModel.TopKhachHangThanThiet = topKhachHangsData.Select(kh => new TopCustomerReportItem
            {
                MaKH = kh.MaKH,
                TenKhachHang = kh.HoTen,
                SoDienThoai = kh.SoDienThoai ?? "N/A",
                Email = kh.Email,
                SoLanDatPhong = kh.DonDatTrongKy.Count,
                TongChiTieu = kh.TongChiTieu,
                NgayDatGanNhat = kh.NgayDatGanNhat,
                HangThanhVien = kh.TongChiTieu >= 20000000 ? "VIP Kim Cương"
                              : kh.TongChiTieu >= 10000000 ? "VIP Vàng"
                              : kh.TongChiTieu >= 5000000 ? "VIP Bạc"
                              : "Thân thiết"
            }).ToList();

            // =========================================================================
            // BÁO CÁO 3: THỐNG KÊ TRẠNG THÁI ĐƠN ĐẶT PHÒNG (DÙNG ĐỂ VẼ BIỂU ĐỒ)
            // =========================================================================
            var donDatPhongsTrongKy = await _context.DatPhongs
                .AsNoTracking()
                .Where(dp => dp.NgayDat >= fromDate && dp.NgayDat <= toDate)
                .Select(dp => new { dp.MaDatPhong, dp.TrangThai })
                .ToListAsync();

            int tongSoDonDat = donDatPhongsTrongKy.Count;

            var nhomTrangThai = donDatPhongsTrongKy
                .GroupBy(dp => dp.TrangThai?.Trim() ?? "Khac")
                .ToDictionary(g => g.Key, g => g.Count());

            var dinhNghiaTrangThai = new[]
            {
                new { Key = "DaThanhToan", Ten = "Hoàn thành", Mau = "#198754", Badge = "bg-success" },
                new { Key = "DaHoanThanh", Ten = "Hoàn thành", Mau = "#198754", Badge = "bg-success" },
                new { Key = "DangLuuTru",  Ten = "Đang ở",      Mau = "#0d6efd", Badge = "bg-primary" },
                new { Key = "DaCoc",       Ten = "Đã cọc",      Mau = "#0dcaf0", Badge = "bg-info text-dark" },
                new { Key = "ChoXacNhan",  Ten = "Chờ xác nhận", Mau = "#ffc107", Badge = "bg-warning text-dark" },
                new { Key = "DaHuy",       Ten = "Đã hủy",      Mau = "#dc3545", Badge = "bg-danger" }
            };

            var listTrangThaiReport = new List<BookingStatusReportItem>();
            var daXuLyGroup = new HashSet<string>();

            int soLuongHoanThanh = 0;
            if (nhomTrangThai.TryGetValue("DaThanhToan", out int countTT)) soLuongHoanThanh += countTT;
            if (nhomTrangThai.TryGetValue("DaHoanThanh", out int countHT)) soLuongHoanThanh += countHT;

            if (soLuongHoanThanh > 0 || tongSoDonDat == 0)
            {
                listTrangThaiReport.Add(new BookingStatusReportItem
                {
                    TrangThaiRaw = "DaThanhToan",
                    TenHienThi = "Hoàn thành",
                    SoLuong = soLuongHoanThanh,
                    TiLePhanTram = tongSoDonDat > 0 ? Math.Round((double)soLuongHoanThanh / tongSoDonDat * 100, 1) : 0,
                    MauSac = "#198754",
                    BadgeClass = "bg-success"
                });
                daXuLyGroup.Add("DaThanhToan");
                daXuLyGroup.Add("DaHoanThanh");
            }

            foreach (var def in dinhNghiaTrangThai)
            {
                if (daXuLyGroup.Contains(def.Key)) continue;

                nhomTrangThai.TryGetValue(def.Key, out int count);
                listTrangThaiReport.Add(new BookingStatusReportItem
                {
                    TrangThaiRaw = def.Key,
                    TenHienThi = def.Ten,
                    SoLuong = count,
                    TiLePhanTram = tongSoDonDat > 0 ? Math.Round((double)count / tongSoDonDat * 100, 1) : 0,
                    MauSac = def.Mau,
                    BadgeClass = def.Badge
                });
                daXuLyGroup.Add(def.Key);
            }

            foreach (var kvp in nhomTrangThai)
            {
                if (!daXuLyGroup.Contains(kvp.Key))
                {
                    listTrangThaiReport.Add(new BookingStatusReportItem
                    {
                        TrangThaiRaw = kvp.Key,
                        TenHienThi = kvp.Key,
                        SoLuong = kvp.Value,
                        TiLePhanTram = tongSoDonDat > 0 ? Math.Round((double)kvp.Value / tongSoDonDat * 100, 1) : 0,
                        MauSac = "#6c757d",
                        BadgeClass = "bg-secondary"
                    });
                }
            }

            viewModel.ThongKeTrangThaiDatPhong = listTrangThaiReport;

            // =========================================================================
            // TỔNG KẾT CÁC THẺ KPI
            // =========================================================================
            viewModel.TongLuotDatPhong = tongSoDonDat;
            viewModel.TongSoKhachHang = await _context.KhachHangs.CountAsync();
            viewModel.DoanhThuTrungBinhDon = tongSoDonDat > 0
                ? Math.Round(viewModel.TongDoanhThuKyNay / tongSoDonDat, 0)
                : 0m;

            return View("Dashboard", viewModel);
        }
    }
}
