namespace HotelManagement.ViewModels
{
    public class HoaDonCheckoutViewModel
    {
        public int MaDatPhong { get; set; }
        public string SoPhong { get; set; } = string.Empty;
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string TenLoaiPhong { get; set; } = string.Empty;
        public DateTime NgayNhan { get; set; }
        public DateTime NgayTraThucTe { get; set; }
        public int SoDem { get; set; }
        public decimal GiaTheoDem { get; set; }

        public decimal TienPhong { get; set; }
        public decimal TongTienDichVu { get; set; }
        public decimal TiLeThuePercent { get; set; } = 10;
        public decimal TienThue { get; set; }
        public decimal TongThanhToan { get; set; }
        public string PhuongThucThanhToan { get; set; } = "TienMat";

        public string NganHangMa { get; set; } = string.Empty;
        public string SoTaiKhoan { get; set; } = string.Empty;
        public string TenChuTaiKhoan { get; set; } = string.Empty;
        public string NoiDungChuyenKhoan { get; set; } = string.Empty;
        public string VietQrImageUrl { get; set; } = string.Empty;

        public List<ChiTietDichVuItemViewModel> DanhSachDichVu { get; set; } = new();
    }

    public class ChiTietDichVuItemViewModel
    {
        public string TenDichVu { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }
}