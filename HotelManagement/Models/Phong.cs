
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("Phong")]
    public class Phong
    {
        [Key]
        public int MaPhong { get; set; }

        [Required, StringLength(20)]
        public string SoPhong { get; set; } = string.Empty;

        public int MaLoaiPhong { get; set; }

        [Required, StringLength(30)]
        public string TrangThai { get; set; } = "Trong";

        [StringLength(500)]
        public string? GhiChu { get; set; }

        [ForeignKey(nameof(MaLoaiPhong))]
        public LoaiPhong? LoaiPhong { get; set; }

        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
            = new List<ChiTietDatPhong>();

        public ICollection<LichSuPhong> LichSuPhongs { get; set; }
            = new List<LichSuPhong>();
    }
}
