using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("NhanVien")]
    public class NhanVien
    {
        [Key]
        public int MaNV { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(20)]
        public string? GioiTinh { get; set; }

        public DateTime? NgaySinh { get; set; }

        [StringLength(15)]
        public string? SoDienThoai { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? DiaChi { get; set; }

        [Required]
        [StringLength(50)]
        public string ChucVu { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LuongCoBan { get; set; }

        public DateTime NgayVaoLam { get; set; } = DateTime.Today;

        public bool TrangThai { get; set; } = true;
    }
}