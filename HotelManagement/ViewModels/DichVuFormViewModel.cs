using System.ComponentModel.DataAnnotations;
using HotelManagement.Models;

namespace HotelManagement.ViewModels;

public class DichVuFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên hàng hóa.")]
    [StringLength(100)]
    public string TenDichVu { get; set; } = string.Empty;

    [StringLength(10000)]
    public string? MoTa { get; set; }

    [Range(0, 100000000000)]
    public decimal Gia { get; set; }

    [Range(0, 100000000000)]
    public decimal GiaVon { get; set; }

    [Required]
    public string LoaiHang { get; set; } = "DichVu";

    [StringLength(80)]
    public string NhomHang { get; set; } = "Dịch vụ";

    [StringLength(30)]
    public string DonViTinh { get; set; } = "Lần";

    [Range(0, int.MaxValue)]
    public int TonKho { get; set; }

    public bool TrangThai { get; set; } = true;
    public bool YeuThich { get; set; }
    public string? AnhUrl { get; set; }
    public IFormFile? Anh { get; set; }

    public static DichVuFormViewModel From(DichVu item) => new()
    {
        Id = item.Id,
        TenDichVu = item.TenDichVu,
        MoTa = item.MoTa,
        Gia = item.Gia,
        GiaVon = item.GiaVon,
        LoaiHang = item.LoaiHang,
        NhomHang = item.NhomHang,
        DonViTinh = item.DonViTinh,
        TonKho = item.TonKho,
        TrangThai = item.TrangThai,
        YeuThich = item.YeuThich,
        AnhUrl = item.AnhUrl
    };
}
