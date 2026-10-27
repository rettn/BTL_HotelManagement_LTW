using HotelManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels
{
    public class DatPhongViewModel
    {
        public int MaDatPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
        public int MaKH { get; set; }

        public DateTime NgayDat { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng.")]
        public DateTime NgayNhan { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng.")]
        public DateTime NgayTra { get; set; } = DateTime.Today.AddDays(1);

        [Range(0, 1000000000, ErrorMessage = "Tiền cọc không hợp lệ.")]
        public decimal TienCoc { get; set; }

        public string? TrangThai { get; set; } = "ChoXacNhan";

        [StringLength(500)]
        public string? GhiChu { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ít nhất một phòng.")]
        public List<int> MaPhongs { get; set; } = new();

        public List<KhachHangOption> KhachHangs { get; set; } = new();

        public List<PhongOption> Phongs { get; set; } = new();
    }

    public class KhachHangOption
    {
        public int MaKH { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
    }

    public class PhongOption
    {
        public int MaPhong { get; set; }
        public string SoPhong { get; set; } = string.Empty;
        public string TenLoaiPhong { get; set; } = string.Empty;
        public decimal GiaTheoDem { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }
}