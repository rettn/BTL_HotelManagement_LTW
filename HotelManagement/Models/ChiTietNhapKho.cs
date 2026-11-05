using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models;

[Table("ChiTietNhapKho")]
public class ChiTietNhapKho
{
    [Key]
    public int Id { get; set; }

    public int PhieuNhapKhoId { get; set; }
    public int DichVuId { get; set; }
    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DonGiaNhap { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ThanhTien { get; set; }

    [ForeignKey(nameof(PhieuNhapKhoId))]
    public PhieuNhapKho? PhieuNhapKho { get; set; }

    [ForeignKey(nameof(DichVuId))]
    public DichVu? DichVu { get; set; }
}
