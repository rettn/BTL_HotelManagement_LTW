using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models;

[Table("PhieuNhapKho")]
public class PhieuNhapKho
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string MaPhieu { get; set; } = string.Empty;

    public DateTime NgayNhap { get; set; } = DateTime.Now;

    [StringLength(120)]
    public string? NhaCungCap { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TongTien { get; set; }

    public ICollection<ChiTietNhapKho> ChiTietNhapKhos { get; set; } = new List<ChiTietNhapKho>();
}
