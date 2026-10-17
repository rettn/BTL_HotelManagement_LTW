
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("LuuTru")]
    public class LuuTru
    {
        [Key]
        public int MaLuuTru { get; set; }

        public int MaChiTietDatPhong { get; set; }

        public DateTime? ThoiGianNhanPhong { get; set; }

        public DateTime? ThoiGianTraPhong { get; set; }

        public int? SoNguoiO { get; set; }

        [StringLength(500)]
        public string? GhiChu { get; set; }

        [ForeignKey(nameof(MaChiTietDatPhong))]
        public ChiTietDatPhong? ChiTietDatPhong { get; set; }

        public ICollection<ChiTietDichVu> ChiTietDichVus { get; set; }
            = new List<ChiTietDichVu>();
    }
}
