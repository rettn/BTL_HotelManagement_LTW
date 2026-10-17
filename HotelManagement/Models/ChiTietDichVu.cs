
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("ChiTietDichVu")]
    public class ChiTietDichVu
    {
        [Key]
        public int MaChiTietDichVu { get; set; }

        public int MaLuuTru { get; set; }

        public int MaDichVu { get; set; }

        public int SoLuong { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        public DateTime ThoiGianSuDung { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? GhiChu { get; set; }

        [ForeignKey(nameof(MaLuuTru))]
        public LuuTru? LuuTru { get; set; }

        [ForeignKey(nameof(MaDichVu))]
        public DichVu? DichVu { get; set; }
    }
}
