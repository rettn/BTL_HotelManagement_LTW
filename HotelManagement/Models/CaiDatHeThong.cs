using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class CaiDatHeThong
    {
        [Key]
        public int Id { get; set; } // ID luôn = 1 vì ta chỉ dùng 1 dòng duy nhất cho hệ thống

        // 1. Thông tin khách sạn
        public string? TenKhachSan { get; set; }
        public string? DiaChi { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? ThongTinHoaDon { get; set; }

        // 2. Quy định lưu trú
        public string? GioNhanPhong { get; set; } = "14:00";
        public string? GioTraPhong { get; set; } = "12:00";
        public int ThoiGianTraMuon { get; set; } = 6;
        public int TienCocMacDinh { get; set; } = 30;
        public string? ChinhSachHuy { get; set; }

        // 3. Cấu hình đặt phòng
        public int MaxDatTruoc { get; set; } = 90;
        public int SoKhachMacDinh { get; set; } = 2;
        public int ThoiHanGiuPhong { get; set; } = 15;

        // 4. Thông báo
        public bool ThongBaoDatMoi { get; set; } = true;
        public bool ThongBaoSapNhanTra { get; set; } = true;
        public bool ThongBaoKho { get; set; } = false;
        public string? KenhThongBao { get; set; } = "System"; // Phân cách bằng dấu phẩy nếu nhiều kênh
    }
}