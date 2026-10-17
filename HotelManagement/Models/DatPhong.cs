
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("DatPhong")]
    public class DatPhong
    {
        [Key]
        public int MaDatPhong { get; set; }

        public int MaKH { get; set; }

        public DateTime NgayDat { get; set; } = DateTime.Now;

        public DateTime NgayNhanDuKien { get; set; }

        public DateTime NgayTraDuKien { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienCoc { get; set; } = 0;

        [Required, StringLength(30)]
        public string TrangThai { get; set; } = "ChoXacNhan";

        [StringLength(500)]
        public string? GhiChu { get; set; }

        [ForeignKey(nameof(MaKH))]
        public KhachHang? KhachHang { get; set; }

        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
            = new List<ChiTietDatPhong>();

        public ICollection<HoaDon> HoaDons { get; set; }
            = new List<HoaDon>();
    }
}
