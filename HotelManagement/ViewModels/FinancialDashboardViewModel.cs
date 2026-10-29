namespace HotelManagement.ViewModels
{
    public class FinancialDashboardViewModel
    {
        // 4 chỉ số doanh thu chính
        public decimal DoanhThuHomNay { get; set; }
        public decimal DoanhThuTuanNay { get; set; }
        public decimal DoanhThuThangNay { get; set; }
        public decimal TongDoanhThu { get; set; }

        // Tỷ lệ cơ cấu doanh thu
        public decimal DoanhThuPhong { get; set; }
        public decimal DoanhThuDichVu { get; set; }
        public double TiLeDoanhThuPhong { get; set; }
        public double TiLeDoanhThuDichVu { get; set; }

        // Dữ liệu biểu đồ 7 ngày gần nhất (truyền cho Chart.js)
        public List<string> Labels7Ngay { get; set; } = new();
        public List<decimal> DoanhThu7Ngay { get; set; } = new();
        public List<decimal> DoanhThuPhong7Ngay { get; set; } = new();
        public List<decimal> DoanhThuDichVu7Ngay { get; set; } = new();

        // Số liệu tổng quan hoạt động
        public int TongHoaDonDaThu { get; set; }
    }
}

