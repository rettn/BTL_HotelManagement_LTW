namespace HotelManagement.ViewModels
{
    public class DashboardTaiChinhViewModel
    {
        public decimal DoanhThuHomNay { get; set; }
        public decimal DoanhThuTuanNay { get; set; }
        public decimal DoanhThuThangNay { get; set; }
        public decimal TongDoanhThu { get; set; }

        // Dữ liệu cho biểu đồ cột 7 ngày
        public List<string> Labels7Ngay { get; set; } = new();
        public List<decimal> DoanhThuPhong7Ngay { get; set; } = new();
        public List<decimal> DoanhThuDichVu7Ngay { get; set; } = new();

        // Dữ liệu cho biểu đồ cơ cấu doanh thu
        public decimal DoanhThuPhong { get; set; }
        public decimal DoanhThuDichVu { get; set; }
        public double TiLeDoanhThuPhong { get; set; }
        public double TiLeDoanhThuDichVu { get; set; }
    }
}
