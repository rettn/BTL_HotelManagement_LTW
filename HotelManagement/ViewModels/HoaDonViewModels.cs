using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels
{
    public class HoaDonItemViewModel
    {
        public int MaHD { get; set; }
        public int MaDatPhong { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string DanhSachPhong { get; set; } = string.Empty;
        public DateTime NgayLap { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; } = "ChuaThanhToan";
        public string? PhuongThucThanhToan { get; set; }
    }

    public class HoaDonIndexViewModel
    {
        public string? TuKhoa { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }

        public List<HoaDonItemViewModel> DanhSachHoaDon { get; set; } = new();

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / (PageSize > 0 ? PageSize : 10));
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public int TongSoHoaDon { get; set; }
        public decimal TongTienDaThu { get; set; }
    }

    public class HoaDonDetailsViewModel
    {
        public int MaHD { get; set; }
        public int MaDatPhong { get; set; }
        public DateTime NgayLap { get; set; }
        public string TrangThai { get; set; } = string.Empty;

        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? CCCD { get; set; }

        public DateTime NgayNhanPhong { get; set; }
        public DateTime NgayTraPhong { get; set; }
        public List<string> PhongDaThue { get; set; } = new();

        public decimal TienPhong { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TongTien { get; set; }

        public List<ChiTietKhoanThuViewModel> ChiTietKhoanThus { get; set; } = new();
        public List<LichSuThanhToanViewModel> LichSuThanhToans { get; set; } = new();
    }

    public class ChiTietKhoanThuViewModel
    {
        public string TenKhoanThu { get; set; } = string.Empty;
        public string LoaiKhoanThu { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class LichSuThanhToanViewModel
    {
        public int MaThanhToan { get; set; }
        public decimal SoTien { get; set; }
        public string PhuongThuc { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public DateTime NgayThanhToan { get; set; }
        public string? MaGiaoDich { get; set; }
    }
}

