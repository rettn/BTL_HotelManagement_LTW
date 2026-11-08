using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models;

[Table("LichCaLamViec")]
public class LichCaLamViec
{
    [Key]
    public int Id { get; set; }

    public int MaNV { get; set; }
    public int MaCa { get; set; }

    [Column(TypeName = "date")]
    public DateTime NgayLam { get; set; }

    [Required, StringLength(20)]
    public string TrangThai { get; set; } = "ChoXacNhan";

    [StringLength(250)]
    public string? GhiChu { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.Now;

    [ForeignKey(nameof(MaNV))]
    public NhanVien? NhanVien { get; set; }

    [ForeignKey(nameof(MaCa))]
    public CaLamViec? CaLamViec { get; set; }
}
