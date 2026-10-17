
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("LichSuPhong")]
    public class LichSuPhong
    {
        [Key]
        public int MaLichSuPhong { get; set; }

        public int MaPhong { get; set; }

        public int? MaNV { get; set; }

        [StringLength(30)]
        public string? TrangThaiCu { get; set; }

        [Required, StringLength(30)]
        public string TrangThaiMoi { get; set; } = string.Empty;

        public DateTime ThoiGianThayDoi { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? LyDo { get; set; }

        [ForeignKey(nameof(MaPhong))]
        public Phong? Phong { get; set; }

        [ForeignKey(nameof(MaNV))]
        public NhanVien? NhanVien { get; set; }
    }
}
