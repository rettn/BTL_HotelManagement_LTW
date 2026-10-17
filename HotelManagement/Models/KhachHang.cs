
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("KhachHang")]
    public class KhachHang
    {
        [Key]
        public int MaKH { get; set; }

        [Required, StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(20)]
        public string? GioiTinh { get; set; }

        public DateTime? NgaySinh { get; set; }

        [StringLength(20)]
        public string? SoDienThoai { get; set; }

        [StringLength(100), EmailAddress]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? DiaChi { get; set; }

        [StringLength(30)]
        public string? CCCD { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public ICollection<DatPhong> DanhSachDatPhong { get; set; }
            = new List<DatPhong>();
    }
}
