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

        // Helper chuẩn hóa khoảng thời gian lọc dùng chung cho cả 4 trang
        private (DateTime from, DateTime to, DateTime rawStart, DateTime rawEnd) NormalizeDateRange(
            DateTime? startDate, DateTime? endDate, DateTime? fromDate, DateTime? toDate)
        {
            var today = DateTime.Today;
            var start = startDate ?? fromDate ?? today.AddDays(-30);
            var end = endDate ?? toDate ?? today;

            if (start > end)
            {
                var temp = start;
                start = end;
                end = temp;
            }

            var filterFrom = start.Date;
            var filterTo = end.Date.AddDays(1).AddTicks(-1);

            return (filterFrom, filterTo, start, end);
        }

        // =========================================================================
        // 1. TRANG TỔNG QUAN CHUNG (/Report/Overview hoặc /Report)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, DateTime? fromDate, DateTime? toDate)
        {
            return await Overview(startDate, endDate, fromDate, toDate);
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(DateTime? startDate, DateTime? endDate, DateTime? fromDate, DateTime? toDate)
        {
            return await Overview(startDate, endDate, fromDate, toDate);
        }

        [HttpGet]
        public async Task<IActionResult> Overview(DateTime? startDate, DateTime? endDate, DateTime? fromDate, DateTime? toDate)
        {
            var (filterFrom, filterTo, rawStart, rawEnd) = NormalizeDateRange(startDate, endDate, fromDate, toDate);
            var today = DateTime.Today;

            var viewModel = new ReportOverviewViewModel
            {
                StartDate = rawStart,
                EndDate = rawEnd
            };

            // Tập hóa đơn đã thanh toán hợp lệ
            var queryPaidInvoices = _context.HoaDons
                .AsNoTracking()
                .Where(h => h.TrangThai == "DaThanhToan");

            var hoaDonTrongKy = await queryPaidInvoices
                .Where(h => h.NgayLap >= filterFrom && h.NgayLap <= filterTo)
                .Select(h => new { h.MaHD, h.TienPhong, h.TienDichVu, h.TongTien, h.NgayLap })
                .ToListAsync();

            decimal tongDoanhThuSauThue = hoaDonTrongKy.Sum(h => h.TongTien);
            viewModel.TongDoanhThuKyNay = tongDoanhThuSauThue;

            // Thống kê Doanh thu Hôm nay, Tuần này, Tháng này
            viewModel.DoanhThuHomNay = await queryPaidInvoices
                .Where(h => h.NgayLap >= today)
                .SumAsync(h => (decimal?)h.TongTien) ?? 0m;

            var dayOfWeek = (int)today.DayOfWeek;
            var startOfWeek = today.AddDays(-((dayOfWeek == 0 ? 7 : dayOfWeek) - 1));
            viewModel.DoanhThuTuanNay = await queryPaidInvoices
                .Where(h => h.NgayLap >= startOfWeek)
                .SumAsync(h => (decimal?)h.TongTien) ?? 0m;

            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            viewModel.DoanhThuThangNay = await queryPaidInvoices
                .Where(h => h.NgayLap >= startOfMonth)
                .SumAsync(h => (decimal?)h.TongTien) ?? 0m;

            // Phân bổ VAT/phí phụ thu khớp 100%
            decimal tongTienPhongChuaThue = hoaDonTrongKy.Sum(h => h.TienPhong);
            decimal tongTienDichVuChuaThue = hoaDonTrongKy.Sum(h => h.TienDichVu);
            decimal tongChuaThue = tongTienPhongChuaThue + tongTienDichVuChuaThue;

            decimal dtPhongPhanBo = 0m;
            decimal dtDichVuPhanBo = 0m;

            if (tongChuaThue > 0)
            {
                dtPhongPhanBo = Math.Round(tongDoanhThuSauThue * (tongTienPhongChuaThue / tongChuaThue), 0);
                dtDichVuPhanBo = tongDoanhThuSauThue - dtPhongPhanBo;
            }

            viewModel.DoanhThuPhong = dtPhongPhanBo;
            viewModel.DoanhThuDichVu = dtDichVuPhanBo;
            viewModel.TiLeDoanhThuPhong = tongDoanhThuSauThue > 0 ? Math.Round((double)(dtPhongPhanBo / tongDoanhThuSauThue * 100), 1) : 0;
            viewModel.TiLeDoanhThuDichVu = tongDoanhThuSauThue > 0 ? Math.Round(100 - viewModel.TiLeDoanhThuPhong, 1) : 0;

            // Gom nhóm biểu đồ linh hoạt
            int soNgayKhoangLoc = (int)(rawEnd.Date - rawStart.Date).TotalDays;
            var labelsBieuDo = new List<string>();
            var dtPhongBieuDo = new List<decimal>();
            var dtDichVuBieuDo = new List<decimal>();
            var dtTongBieuDo = new List<decimal>();

            if (soNgayKhoangLoc <= 7)
            {
                viewModel.LoaiGomNhom = "Ngay";
                viewModel.TieuDeBieuDoCot = $"Doanh thu theo ngày ({rawStart:dd/MM} - {rawEnd:dd/MM})";

                for (var d = rawStart.Date; d <= rawEnd.Date; d = d.AddDays(1))
                {
                    labelsBieuDo.Add(d.ToString("dd/MM"));
                    var inDay = hoaDonTrongKy.Where(h => h.NgayLap.Date == d).ToList();
                    decimal tongTrongNgay = inDay.Sum(h => h.TongTien);
                    decimal pChuaThue = inDay.Sum(h => h.TienPhong);
                    decimal dvChuaThue = inDay.Sum(h => h.TienDichVu);
                    decimal tongChuaThueNgay = pChuaThue + dvChuaThue;

                    decimal pVal = tongChuaThueNgay > 0 ? Math.Round(tongTrongNgay * (pChuaThue / tongChuaThueNgay), 0) : 0m;
                    decimal dvVal = tongTrongNgay - pVal;

                    dtPhongBieuDo.Add(pVal);
                    dtDichVuBieuDo.Add(dvVal);
                    dtTongBieuDo.Add(tongTrongNgay);
                }
            }
            else if (soNgayKhoangLoc <= 30)
            {
                viewModel.LoaiGomNhom = "Tuan";
                viewModel.TieuDeBieuDoCot = $"Doanh thu gom theo tuần ({rawStart:dd/MM} - {rawEnd:dd/MM})";

                int tuanIndex = 1;
                for (var d = rawStart.Date; d <= rawEnd.Date; d = d.AddDays(7))
                {
                    var weekEnd = d.AddDays(6) > rawEnd.Date ? rawEnd.Date : d.AddDays(6);
                    labelsBieuDo.Add($"T{tuanIndex} ({d:dd/MM}-{weekEnd:dd/MM})");

                    var inWeek = hoaDonTrongKy.Where(h => h.NgayLap.Date >= d && h.NgayLap.Date <= weekEnd).ToList();
                    decimal tongTuan = inWeek.Sum(h => h.TongTien);
                    decimal pChuaThue = inWeek.Sum(h => h.TienPhong);
                    decimal dvChuaThue = inWeek.Sum(h => h.TienDichVu);
                    decimal tongChuaThueTuan = pChuaThue + dvChuaThue;

                    decimal pVal = tongChuaThueTuan > 0 ? Math.Round(tongTuan * (pChuaThue / tongChuaThueTuan), 0) : 0m;
                    decimal dvVal = tongTuan - pVal;

                    dtPhongBieuDo.Add(pVal);
                    dtDichVuBieuDo.Add(dvVal);
                    dtTongBieuDo.Add(tongTuan);
                    tuanIndex++;
                }
            }
            else
            {
                viewModel.LoaiGomNhom = "Thang";
                viewModel.TieuDeBieuDoCot = $"Doanh thu gom theo tháng ({rawStart:MM/yyyy} - {rawEnd:MM/yyyy})";

                var curMonth = new DateTime(rawStart.Year, rawStart.Month, 1);
                var endMonth = new DateTime(rawEnd.Year, rawEnd.Month, 1);

                while (curMonth <= endMonth)
                {
                    labelsBieuDo.Add(curMonth.ToString("MM/yyyy"));
                    var nextMonth = curMonth.AddMonths(1);

                    var inMonth = hoaDonTrongKy.Where(h => h.NgayLap >= curMonth && h.NgayLap < nextMonth).ToList();
                    decimal tongThang = inMonth.Sum(h => h.TongTien);
                    decimal pChuaThue = inMonth.Sum(h => h.TienPhong);
                    decimal dvChuaThue = inMonth.Sum(h => h.TienDichVu);
                    decimal tongChuaThueThang = pChuaThue + dvChuaThue;

                    decimal pVal = tongChuaThueThang > 0 ? Math.Round(tongThang * (pChuaThue / tongChuaThueThang), 0) : 0m;
                    decimal dvVal = tongThang - pVal;

                    dtPhongBieuDo.Add(pVal);
                    dtDichVuBieuDo.Add(dvVal);
                    dtTongBieuDo.Add(tongThang);

                    curMonth = nextMonth;
                }
            }

            viewModel.LabelsThoiGian = labelsBieuDo;
            viewModel.DoanhThuPhongThoiGian = dtPhongBieuDo;
            viewModel.DoanhThuDichVuThoiGian = dtDichVuBieuDo;
            viewModel.TongDoanhThuThoiGian = dtTongBieuDo;

            // Thống kê trạng thái đơn đặt phòng
            var donDatPhongsTrongKy = await _context.DatPhongs
                .AsNoTracking()
                .Where(dp => dp.NgayDat >= filterFrom && dp.NgayDat <= filterTo)
                .Select(dp => new { dp.MaDatPhong, dp.TrangThai })
                .ToListAsync();

            int tongSoDonDat = donDatPhongsTrongKy.Count;
            viewModel.TongLuotDatPhong = tongSoDonDat;

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

            viewModel.ThongKeTrangThaiDatPhong = listTrangThaiReport;

            return View("Overview", viewModel);
        }

        // =========================================================================
        // 2. TRANG CÔNG SUẤT & HẠNG PHÒNG (/Report/RoomPerformance)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> CongSuatPhong(DateTime? startDate, DateTime? endDate, DateTime? fromDate, DateTime? toDate)
        {
            return await RoomPerformance(startDate, endDate, fromDate, toDate);
        }

        [HttpGet]
        public async Task<IActionResult> RoomPerformance(DateTime? startDate, DateTime? endDate, DateTime? fromDate, DateTime? toDate)
        {
            var (filterFrom, filterTo, rawStart, rawEnd) = NormalizeDateRange(startDate, endDate, fromDate, toDate);

            var viewModel = new RoomPerformanceViewModel
            {
                StartDate = rawStart,
                EndDate = rawEnd
            };

            int tongSoPhong = await _context.Phongs.CountAsync();
            viewModel.TongSoPhong = tongSoPhong;

            int soNgayKhoangLoc = Math.Max(1, (int)(rawEnd.Date - rawStart.Date).TotalDays + 1);
            int tongSoDemKhaDung = tongSoPhong * soNgayKhoangLoc;
            viewModel.TongSoDemKhaDung = tongSoDemKhaDung;

            // Lấy chi tiết đặt phòng
            var chiTietDatPhongs = await _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                    .ThenInclude(p => p.LoaiPhong)
                .Include(ct => ct.DatPhong)
                .Where(ct => ct.DatPhong != null 
                          && ct.DatPhong.TrangThai != "DaHuy"
                          && ct.NgayNhan <= filterTo 
                          && ct.NgayTra >= filterFrom)
                .ToListAsync();

            // Tính số đêm phòng thực tế có khách lưu trú trong khoảng thời gian lọc
            int tongSoDemThucTe = chiTietDatPhongs.Sum(ct =>
            {
                var checkIn = ct.NgayNhan > filterFrom ? ct.NgayNhan : filterFrom;
                var checkOut = ct.NgayTra < filterTo ? ct.NgayTra : filterTo;
                if (checkOut <= checkIn) return 1;
                return Math.Max(1, (int)(checkOut.Date - checkIn.Date).TotalDays);
            });

            viewModel.TongSoDemThucTe = tongSoDemThucTe;
            viewModel.OccupancyRate = tongSoDemKhaDung > 0 
                ? Math.Min(100.0, Math.Round((double)tongSoDemThucTe / tongSoDemKhaDung * 100, 1)) 
                : 0.0;

            // Bảng Doanh thu theo từng hạng phòng
            var allLoaiPhongs = await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(lp => lp.MaLoaiPhong)
                .ToListAsync();

            var listDoanhThuHangPhong = new List<RoomCategoryRevenueItem>();
            foreach (var lp in allLoaiPhongs)
            {
                var dsChiTietCuaLoai = chiTietDatPhongs
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
            viewModel.TongDoanhThuPhong = tongDoanhThuPhong;
            viewModel.TongLuotDatPhong = listDoanhThuHangPhong.Sum(x => x.SoLuotDat);

            foreach (var item in listDoanhThuHangPhong)
            {
                item.TiLePhanTram = tongDoanhThuPhong > 0
                    ? Math.Round((double)(item.DoanhThu / tongDoanhThuPhong * 100), 1)
                    : 0;
            }

            viewModel.DoanhThuTheoHangPhong = listDoanhThuHangPhong.OrderByDescending(x => x.DoanhThu).ToList();

            return View("RoomPerformance", viewModel);
        }

        // =========================================================================
        // 3. TRANG DOANH THU DỊCH VỤ (/Report/ServiceRevenue)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> DoanhThuDichVu(DateTime? startDate, DateTime? endDate, DateTime? fromDate, DateTime? toDate)
        {
            return await ServiceRevenue(startDate, endDate, fromDate, toDate);
        }

        [HttpGet]
        public async Task<IActionResult> ServiceRevenue(DateTime? startDate, DateTime? endDate, DateTime? fromDate, DateTime? toDate)
        {
            var (filterFrom, filterTo, rawStart, rawEnd) = NormalizeDateRange(startDate, endDate, fromDate, toDate);

            var viewModel = new ServiceRevenueViewModel
            {
                StartDate = rawStart,
                EndDate = rawEnd
            };

            // Truy vấn các lượt sử dụng dịch vụ trong kỳ
            var chiTietDVs = await _context.ChiTietDichVus
                .AsNoTracking()
                .Include(ct => ct.DichVu)
                .Where(ct => ct.ThoiGianSuDung >= filterFrom && ct.ThoiGianSuDung <= filterTo)
                .ToListAsync();

            decimal tongDoanhThuDichVu = chiTietDVs.Sum(ct => ct.SoLuong * ct.DonGia);
            viewModel.TongDoanhThuDichVu = tongDoanhThuDichVu;
            viewModel.TongLuotSuDung = chiTietDVs.Sum(ct => ct.SoLuong);

            // Gom nhóm theo từng mặt hàng dịch vụ
            var listDichVu = chiTietDVs
                .GroupBy(ct => new
                {
                    ct.MaDichVu,
                    Ten = ct.DichVu != null ? ct.DichVu.TenDichVu : "Dịch vụ khác",
                    MaHang = ct.DichVu != null ? ct.DichVu.MaHang : $"DV{ct.MaDichVu:D3}",
                    Nhom = ct.DichVu != null && !string.IsNullOrWhiteSpace(ct.DichVu.NhomHang) ? ct.DichVu.NhomHang : "Dịch vụ chung",
                    DonVi = ct.DichVu != null ? ct.DichVu.DonViTinh : "Lần"
                })
                .Select(g =>
                {
                    decimal dtItem = g.Sum(x => x.SoLuong * x.DonGia);
                    int slItem = g.Sum(x => x.SoLuong);
                    decimal donGiaTrungBinh = slItem > 0 ? Math.Round(dtItem / slItem, 0) : 0m;

                    return new ServiceRevenueItem
                    {
                        MaDichVu = g.Key.MaDichVu,
                        MaHang = g.Key.MaHang,
                        TenDichVu = g.Key.Ten,
                        NhomHang = g.Key.Nhom,
                        DonViTinh = g.Key.DonVi,
                        DonGia = donGiaTrungBinh,
                        SoLuongSuDung = slItem,
                        DoanhThu = dtItem,
                        TiLePhanTram = tongDoanhThuDichVu > 0 ? Math.Round((double)(dtItem / tongDoanhThuDichVu * 100), 1) : 0
                    };
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToList();

            viewModel.DanhSachDichVu = listDichVu;
            viewModel.TongSoMatHang = listDichVu.Count;

            // Gom nhóm cơ cấu theo Nhóm Hàng (F&B, Giặt là, Spa...)
            var colors = new[] { "#ff9800", "#00bcd4", "#e91e63", "#0d6efd", "#4caf50", "#9c27b0" };
            int colorIndex = 0;

            var nhomDVs = listDichVu
                .GroupBy(d => d.NhomHang)
                .Select(g => new ServiceCategoryRevenueItem
                {
                    TenNhom = g.Key,
                    DoanhThu = g.Sum(x => x.DoanhThu),
                    SoLuotSuDung = g.Sum(x => x.SoLuongSuDung),
                    TiLePhanTram = tongDoanhThuDichVu > 0 ? Math.Round((double)(g.Sum(x => x.DoanhThu) / tongDoanhThuDichVu * 100), 1) : 0,
                    MauSac = colors[colorIndex++ % colors.Length]
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToList();

            viewModel.CoCauNhomDichVu = nhomDVs;
            viewModel.NhomHangChuLuc = nhomDVs.FirstOrDefault()?.TenNhom ?? "---";

            return View("ServiceRevenue", viewModel);
        }

        // =========================================================================
        // 4. TRANG TOP KHÁCH HÀNG VIP (/Report/TopCustomers)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> TopKhachHang(DateTime? startDate, DateTime? endDate, DateTime? fromDate, DateTime? toDate)
        {
            return await TopCustomers(startDate, endDate, fromDate, toDate);
        }

        [HttpGet]
        public async Task<IActionResult> TopCustomers(DateTime? startDate, DateTime? endDate, DateTime? fromDate, DateTime? toDate)
        {
            var (filterFrom, filterTo, rawStart, rawEnd) = NormalizeDateRange(startDate, endDate, fromDate, toDate);

            var viewModel = new TopCustomersViewModel
            {
                StartDate = rawStart,
                EndDate = rawEnd
            };

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
                        .Where(dp => dp.NgayDat >= filterFrom && dp.NgayDat <= filterTo)
                        .ToList(),
                    TongChiTieu = kh.DanhSachDatPhong
                        .SelectMany(dp => dp.HoaDons)
                        .Where(h => h.TrangThai == "DaThanhToan"
                                 && h.NgayLap >= filterFrom
                                 && h.NgayLap <= filterTo)
                        .Sum(h => (decimal?)h.TongTien) ?? 0m,
                    NgayDatGanNhat = kh.DanhSachDatPhong
                        .Where(dp => dp.NgayDat >= filterFrom && dp.NgayDat <= filterTo)
                        .OrderByDescending(dp => dp.NgayDat)
                        .Select(dp => (DateTime?)dp.NgayDat)
                        .FirstOrDefault()
                })
                .Where(x => x.TongChiTieu > 0 || x.DonDatTrongKy.Any())
                .OrderByDescending(x => x.TongChiTieu)
                .ThenByDescending(x => x.DonDatTrongKy.Count)
                .ToListAsync();

            viewModel.TongKhachHangChiTieu = topKhachHangsData.Count;
            viewModel.TongChiTieuTop10 = topKhachHangsData.Take(10).Sum(x => x.TongChiTieu);
            viewModel.ChiTieuTrungBinh = topKhachHangsData.Count > 0 
                ? Math.Round(topKhachHangsData.Sum(x => x.TongChiTieu) / topKhachHangsData.Count, 0) 
                : 0m;
            viewModel.KhachHangTieuBieu = topKhachHangsData.FirstOrDefault()?.HoTen ?? "---";

            viewModel.TopKhachHangs = topKhachHangsData.Select(kh => new TopCustomerReportItem
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

            return View("TopCustomers", viewModel);
        }
    }
}
