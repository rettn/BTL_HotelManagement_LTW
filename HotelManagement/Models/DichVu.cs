
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("DichVu")]
    public class DichVu
    {
        [Key]
        public int MaDichVu { get; set; }

        [Required, StringLength(100)]
        public string TenDichVu { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MoTa { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [StringLength(30)]
        public string DonViTinh { get; set; } = "Lan";

        public bool TrangThai { get; set; } = true;

        public ICollection<ChiTietDichVu> ChiTietDichVus { get; set; }
            = new List<ChiTietDichVu>();
    }
}
