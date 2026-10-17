
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("HoanTien")]
    public class HoanTien
    {
        [Key]
        public int MaHoanTien { get; set; }

        public int MaThanhToan { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienHoan { get; set; }

        [Required, StringLength(500)]
        public string LyDo { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string TrangThai { get; set; } = "ChoXuLy";

        public DateTime NgayYeuCau { get; set; } = DateTime.Now;

        public DateTime? NgayHoanTien { get; set; }

        [ForeignKey(nameof(MaThanhToan))]
        public ThanhToan? ThanhToan { get; set; }
    }
}
