
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("HoaDon")]
    public class HoaDon
    {
        [Key]
        public int MaHD { get; set; }

        public int MaDatPhong { get; set; }

        public DateTime NgayLap { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienPhong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienDichVu { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiamGia { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [Required, StringLength(30)]
        public string TrangThai { get; set; } = "ChuaThanhToan";

        [ForeignKey(nameof(MaDatPhong))]
        public DatPhong? DatPhong { get; set; }

        public ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; }
            = new List<ChiTietHoaDon>();

        public ICollection<ThanhToan> ThanhToans { get; set; }
            = new List<ThanhToan>();
    }
}
