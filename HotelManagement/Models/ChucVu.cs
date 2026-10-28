
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("ChucVu")]
    public class ChucVu
    {
        [Key]
        public int MaChucVu { get; set; }

        [Required(ErrorMessage = "Vui long nhap ten chuc vu.")]
        [StringLength(50)]
        public string TenChucVu { get; set; } = string.Empty;

        [StringLength(255)]
        public string? MoTa { get; set; }

        public bool TrangThai { get; set; } = true;
    }
}
