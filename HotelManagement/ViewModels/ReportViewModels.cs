using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HotelManagement.ViewModels
{
    // =========================================================================
    // 1. VIEWMODEL: TRANG TỔNG QUAN CHUNG (/Report/Overview)
    // =========================================================================
    public class ReportOverviewViewModel
    {
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public DateTime? FromDate => StartDate;
        public DateTime? ToDate => EndDate;

        // Thẻ KPI tổng quan
        public decimal TongDoanhThuKyNay { get; set; }
        public decimal DoanhThuHomNay { get; set; }
        public decimal DoanhThuTuanNay { get; set; }
        public decimal DoanhThuThangNay { get; set; }
        public int TongLuotDatPhong { get; set; }

        // Biểu đồ tròn: Cơ cấu doanh thu (Đã phân bổ VAT/Phụ thu khớp 100%)
        public decimal DoanhThuPhong { get; set; }
        public decimal DoanhThuDichVu { get; set; }
        public double TiLeDoanhThuPhong { get; set; }
        public double TiLeDoanhThuDichVu { get; set; }

        // Biểu đồ cột: Biến động doanh thu theo kỳ lọc
        public string TieuDeBieuDoCot { get; set; } = "Biến động Doanh thu";
        public string LoaiGomNhom { get; set; } = "Ngay";
        public List<string> LabelsThoiGian { get; set; } = new();
        public List<decimal> DoanhThuPhongThoiGian { get; set; } = new();
        public List<decimal> DoanhThuDichVuThoiGian { get; set; } = new();
        public List<decimal> TongDoanhThuThoiGian { get; set; } = new();

        // Biểu đồ tròn: Trạng thái đơn đặt phòng
        public List<BookingStatusReportItem> ThongKeTrangThaiDatPhong { get; set; } = new();
    }

    // =========================================================================
    // 2. VIEWMODEL: CÔNG SUẤT & HẠNG PHÒNG (/Report/RoomPerformance)
    // =========================================================================
    public class RoomPerformanceViewModel
    {
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        // Chỉ số công suất khai thác (Occupancy Rate)
        public double OccupancyRate { get; set; }
        public int TongSoPhong { get; set; }
        public int TongSoDemKhaDung { get; set; }
        public int TongSoDemThucTe { get; set; }
        public decimal TongDoanhThuPhong { get; set; }
        public int TongLuotDatPhong { get; set; }

        // Danh sách doanh thu phân rã theo từng hạng phòng
        public List<RoomCategoryRevenueItem> DoanhThuTheoHangPhong { get; set; } = new();
    }

    // =========================================================================
    // 3. VIEWMODEL: DOANH THU DỊCH VỤ (/Report/ServiceRevenue)
    // =========================================================================
    public class ServiceRevenueViewModel
    {
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        // Chỉ số KPI dịch vụ
        public decimal TongDoanhThuDichVu { get; set; }
        public int TongLuotSuDung { get; set; }
        public int TongSoMatHang { get; set; }
        public string NhomHangChuLuc { get; set; } = "F&B";

        // Cơ cấu doanh thu theo nhóm dịch vụ (F&B, Giặt là, Spa, Minibar...)
        public List<ServiceCategoryRevenueItem> CoCauNhomDichVu { get; set; } = new();

        // Danh sách chi tiết từng mặt hàng dịch vụ
        public List<ServiceRevenueItem> DanhSachDichVu { get; set; } = new();
    }

    public class ServiceCategoryRevenueItem
    {
        public string TenNhom { get; set; } = string.Empty;
        public decimal DoanhThu { get; set; }
        public int SoLuotSuDung { get; set; }
        public double TiLePhanTram { get; set; }
        public string MauSac { get; set; } = "#0d6efd";
    }

    public class ServiceRevenueItem
    {
        public int MaDichVu { get; set; }
        public string MaHang { get; set; } = string.Empty;
        public string TenDichVu { get; set; } = string.Empty;
        public string NhomHang { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = "Lần";
        public decimal DonGia { get; set; }
        public int SoLuongSuDung { get; set; }
        public decimal DoanhThu { get; set; }
        public double TiLePhanTram { get; set; }
    }

    // =========================================================================
    // 4. VIEWMODEL: TOP KHÁCH HÀNG VIP (/Report/TopCustomers)
    // =========================================================================
    public class TopCustomersViewModel
    {
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        // Chỉ số KPI khách hàng
        public int TongKhachHangChiTieu { get; set; }
        public decimal TongChiTieuTop10 { get; set; }
        public decimal ChiTieuTrungBinh { get; set; }
        public string KhachHangTieuBieu { get; set; } = "---";

        // Danh sách bảng xếp hạng Top khách hàng
        public List<TopCustomerReportItem> TopKhachHangs { get; set; } = new();
    }
}

