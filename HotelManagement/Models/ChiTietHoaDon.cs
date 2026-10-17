
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("ChiTietHoaDon")]
    public class ChiTietHoaDon
    {
        [Key]
        public int MaChiTietHD { get; set; }

        public int MaHD { get; set; }

        [Required, StringLength(100)]
        public string TenKhoanThu { get; set; } = string.Empty;

        [StringLength(30)]
        public string LoaiKhoanThu { get; set; } = "Khac";

        public int? MaThamChieu { get; set; }

        public int SoLuong { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ThanhTien { get; set; }

        [ForeignKey(nameof(MaHD))]
        public HoaDon? HoaDon { get; set; }
    }
}
