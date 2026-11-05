using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels;

public class NhapKhoFormViewModel
{
    [StringLength(120)]
    public string? NhaCungCap { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [MinLength(1, ErrorMessage = "Vui lòng thêm ít nhất một mặt hàng.")]
    public List<NhapKhoDongViewModel> DongHang { get; set; } = new();
}

public class NhapKhoDongViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn mặt hàng.")]
    public int DichVuId { get; set; }

    [Range(1, 100000, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int SoLuong { get; set; }

    [Range(0, 100000000000, ErrorMessage = "Đơn giá không hợp lệ.")]
    public decimal DonGiaNhap { get; set; }
}
