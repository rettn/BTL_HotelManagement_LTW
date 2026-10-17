
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("NhatKyHeThong")]
    public class NhatKyHeThong
    {
        [Key]
        public long MaNhatKy { get; set; }

        // Id cua nguoi dung ASP.NET Identity
        [StringLength(450)]
        public string? MaNguoiDung { get; set; }

        [Required, StringLength(100)]
        public string HanhDong { get; set; } = string.Empty;

        [StringLength(100)]
        public string? BangDuLieu { get; set; }

        [StringLength(100)]
        public string? MaBanGhi { get; set; }

        [StringLength(2000)]
        public string? NoiDung { get; set; }

        [StringLength(50)]
        public string? DiaChiIP { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }
}
