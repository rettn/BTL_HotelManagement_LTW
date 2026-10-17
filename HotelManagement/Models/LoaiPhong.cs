
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("LoaiPhong")]
    public class LoaiPhong
    {
        [Key]
        public int MaLoaiPhong { get; set; }

        [Required, StringLength(100)]
        public string TenLoaiPhong { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MoTa { get; set; }

        [Range(1, 100)]
        public int SoNguoiToiDa { get; set; } = 2;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaTheoDem { get; set; }

        public ICollection<Phong> DanhSachPhong { get; set; }
            = new List<Phong>();
    }
}
