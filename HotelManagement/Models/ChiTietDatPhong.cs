
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("ChiTietDatPhong")]
    public class ChiTietDatPhong
    {
        [Key]
        public int MaChiTietDatPhong { get; set; }

        public int MaDatPhong { get; set; }

        public int MaPhong { get; set; }

        public DateTime NgayNhan { get; set; }

        public DateTime NgayTra { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [ForeignKey(nameof(MaDatPhong))]
        public DatPhong? DatPhong { get; set; }

        [ForeignKey(nameof(MaPhong))]
        public Phong? Phong { get; set; }

        public ICollection<LuuTru> DanhSachLuuTru { get; set; }
            = new List<LuuTru>();
    }
}
