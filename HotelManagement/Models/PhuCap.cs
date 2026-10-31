using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("PhuCap")]
    public class PhuCap
    {
        [Key]
        public int MaPhuCap { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nhân viên.")]
        public int MaNV { get; set; }

        [Range(1, 12, ErrorMessage = "Tháng phải từ 1 đến 12.")]
        public int Thang { get; set; }

        [Range(2000, 2100, ErrorMessage = "Năm phải từ 2000 đến 2100.")]
        public int Nam { get; set; }

        [Required, StringLength(150)]
        public string TenPhuCap { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Số tiền phải lớn hơn 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; }

        [StringLength(500)]
        public string? LyDo { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [ForeignKey(nameof(MaNV))]
        public NhanVien? NhanVien { get; set; }
    }
}
