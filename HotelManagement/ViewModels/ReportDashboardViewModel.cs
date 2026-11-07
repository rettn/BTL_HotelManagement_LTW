using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels
{
    public class ReportDashboardViewModel
    {
        // 1. Bộ lọc thời gian
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        // Alias hỗ trợ cả FromDate / ToDate
        public DateTime? FromDate => StartDate;
        public DateTime? ToDate => EndDate;

        // 2. Thẻ số liệu tổng quan (KPI Cards)
        public decimal TongDoanhThuKyNay { get; set; }
        public decimal DoanhThuHomNay { get; set; }
        public decimal DoanhThuTuanNay { get; set; }
        public decimal DoanhThuThangNay { get; set; }
        public int TongLuotDatPhong { get; set; }
        public int TongSoKhachHang { get; set; }
        public decimal DoanhThuTrungBinhDon { get; set; }

        // 3. Cơ cấu doanh thu (Biểu đồ tròn - đã phân bổ VAT/phí phụ thu khớp 100% với thẻ tổng)
        public decimal DoanhThuPhong { get; set; }
        public decimal DoanhThuDichVu { get; set; }
        public double TiLeDoanhThuPhong { get; set; }
        public double TiLeDoanhThuDichVu { get; set; }

        // 4. Biến động doanh thu theo kỳ lọc (Gom nhóm linh hoạt: Ngày / Tuần / Tháng)
        public string TieuDeBieuDoCot { get; set; } = "Biến động Doanh thu";
        public string LoaiGomNhom { get; set; } = "Ngay"; // "Ngay", "Tuan", "Thang"
        public List<string> Labels7Ngay { get; set; } = new();
        public List<decimal> DoanhThuPhong7Ngay { get; set; } = new();
        public List<decimal> DoanhThuDichVu7Ngay { get; set; } = new();
        public List<decimal> DoanhThu7Ngay { get; set; } = new();

        // 5. Phần 1: Báo cáo doanh thu theo hạng phòng
        public List<RoomCategoryRevenueItem> DoanhThuTheoHangPhong { get; set; } = new();

        // 6. Phần 2: Báo cáo Top khách hàng thân thiết
        public List<TopCustomerReportItem> TopKhachHangThanThiet { get; set; } = new();

        // 7. Phần 3: Thống kê trạng thái đơn đặt phòng (Dữ liệu vẽ biểu đồ Chart.js)
        public List<BookingStatusReportItem> ThongKeTrangThaiDatPhong { get; set; } = new();

        // Helper truyền trực tiếp vào Chart.js trạng thái
        public List<string> ChartStatusLabels => ThongKeTrangThaiDatPhong.Select(x => x.TenHienThi).ToList();
        public List<int> ChartStatusCounts => ThongKeTrangThaiDatPhong.Select(x => x.SoLuong).ToList();
        public List<string> ChartStatusColors => ThongKeTrangThaiDatPhong.Select(x => x.MauSac).ToList();
    }

    // Item: Doanh thu theo hạng phòng
    public class RoomCategoryRevenueItem
    {
        public int MaLoaiPhong { get; set; }
        public string TenLoaiPhong { get; set; } = string.Empty;
        public decimal GiaTheoDemHienTai { get; set; }
        public int SoLuotDat { get; set; }
        public int TongSoDemThue { get; set; }
        public decimal DoanhThu { get; set; }
        public double TiLePhanTram { get; set; }
    }

    // Item: Top khách hàng chi tiêu
    public class TopCustomerReportItem
    {
        public int MaKH { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int SoLanDatPhong { get; set; }
        public decimal TongChiTieu { get; set; }
        public DateTime? NgayDatGanNhat { get; set; }
        public string HangThanhVien { get; set; } = "Thành viên";
    }

    // Item: Trạng thái đơn đặt phòng
    public class BookingStatusReportItem
    {
        public string TrangThaiRaw { get; set; } = string.Empty;
        public string TenHienThi { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public double TiLePhanTram { get; set; }
        public string MauSac { get; set; } = "#6c757d";
        public string BadgeClass { get; set; } = "bg-secondary";
    }
}
