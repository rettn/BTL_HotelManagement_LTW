using System.ComponentModel.DataAnnotations;
using HotelManagement.Models;

namespace HotelManagement.ViewModels
{
    public class DatPhongKhachHangViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^[0-9\+\-\s]{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Địa chỉ email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số CCCD/Hộ chiếu")]
        public string? CCCD { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime NgayNhan { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày trả phòng")]
        public DateTime NgayTra { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Vui lòng chọn phòng muốn đặt.")]
        [Display(Name = "Phòng đã chọn")]
        public int MaPhong { get; set; }

        [Display(Name = "Ghi chú / Yêu cầu đặc biệt")]
        [StringLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự.")]
        public string? GhiChu { get; set; }

        [Display(Name = "Hình thức thanh toán")]
        public string PhuongThucThanhToan { get; set; } = "VietQR"; // "VietQR" hoặc "TrucTiep"

        // Dữ liệu hiển thị danh sách phòng cho khách chọn
        public List<PhongKhachHangItem> DanhSachPhongTrong { get; set; } = new();
        public List<LoaiPhong> DanhSachLoaiPhong { get; set; } = new();

        // Thuộc tính tính toán
        public int SoDem => (NgayTra.Date - NgayNhan.Date).Days > 0 ? (NgayTra.Date - NgayNhan.Date).Days : 1;
    }

    public class PhongKhachHangItem
    {
        public int MaPhong { get; set; }
        public string SoPhong { get; set; } = string.Empty;
        public int MaLoaiPhong { get; set; }
        public string TenLoaiPhong { get; set; } = string.Empty;
        public decimal GiaTheoDem { get; set; }
        public int SoNguoiToiDa { get; set; }
        public string? MoTa { get; set; }
    }
}
