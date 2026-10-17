
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("ThongBao")]
    public class ThongBao
    {
        [Key]
        public int MaThongBao { get; set; }

        [Required, StringLength(150)]
        public string TieuDe { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string NoiDung { get; set; } = string.Empty;

        // Id cua nguoi dung ASP.NET Identity
        [StringLength(450)]
        public string? MaNguoiDung { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public bool DaDoc { get; set; } = false;

        [StringLength(30)]
        public string LoaiThongBao { get; set; } = "ThongThuong";
    }
}
