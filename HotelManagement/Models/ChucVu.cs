using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("ChucVu")]
    public class ChucVu
    {
        [Key]
        public int MaChucVu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chức vụ.")]
        [StringLength(50)]
        public string TenChucVu { get; set; } = string.Empty;

        [StringLength(255)]
        public string? MoTa { get; set; }

        [Display(Name = "Lương cơ bản mặc định")]
        [Range(0, 999999999999, ErrorMessage = "Lương cơ bản không được âm.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LuongCoBanMacDinh { get; set; }

        public bool TrangThai { get; set; } = true;
    }
}
