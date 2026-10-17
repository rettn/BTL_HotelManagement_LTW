
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("ThanhToan")]
    public class ThanhToan
    {
        [Key]
        public int MaThanhToan { get; set; }

        public int MaHD { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; }

        [Required, StringLength(30)]
        public string PhuongThuc { get; set; } = "TienMat";

        [Required, StringLength(30)]
        public string TrangThai { get; set; } = "ChoXuLy";

        public DateTime NgayThanhToan { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? MaGiaoDich { get; set; }

        [ForeignKey(nameof(MaHD))]
        public HoaDon? HoaDon { get; set; }

        public ICollection<HoanTien> HoanTiens { get; set; }
            = new List<HoanTien>();
    }
}
