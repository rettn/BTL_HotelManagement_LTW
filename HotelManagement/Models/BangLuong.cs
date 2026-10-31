using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("BangLuong")]
    public class BangLuong
    {
        [Key]
        public int MaBangLuong { get; set; }

        public int MaNV { get; set; }

        public int Thang { get; set; }

        public int Nam { get; set; }

        // Luu lai muc luong tai thoi diem tinh luong
        [Column(TypeName = "decimal(18,2)")]
        public decimal LuongCoBan { get; set; }

        public int NgayCongQuyDinh { get; set; } = 26;

        [Column(TypeName = "decimal(8,2)")]
        public decimal NgayCongThucTe { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LuongTheoCong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongPhuCap { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongThuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongKhauTru { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ThucLinh { get; set; }

        [Required, StringLength(30)]
        public string TrangThai { get; set; } = "ChuaThanhToan";

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayThanhToan { get; set; }

        [ForeignKey(nameof(MaNV))]
        public NhanVien? NhanVien { get; set; }
    }
}