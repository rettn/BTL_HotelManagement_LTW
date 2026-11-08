using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models;

[Table("CaLamViec")]
public class CaLamViec
{
    [Key]
    public int MaCa { get; set; }

    [Required, StringLength(80)]
    public string TenCa { get; set; } = string.Empty;

    public TimeSpan GioBatDau { get; set; }
    public TimeSpan GioKetThuc { get; set; }

    [StringLength(7)]
    public string MauSac { get; set; } = "#198754";

    public bool TrangThai { get; set; } = true;

    [StringLength(250)]
    public string? GhiChu { get; set; }

    public ICollection<LichCaLamViec> LichCaLamViecs { get; set; } = new List<LichCaLamViec>();
}
