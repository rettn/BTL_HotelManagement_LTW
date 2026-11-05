using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class DichVu
    {
        [Key]
        public int Id { get; set; }

        [StringLength(20)]
        [Display(Name = "Mã hàng hóa")]
        public string MaHang { get; set; } = string.Empty;

        [Required, StringLength(20)]
        [Display(Name = "Loại hàng")]
        public string LoaiHang { get; set; } = "DichVu";

        [StringLength(80)]
        [Display(Name = "Nhóm hàng")]
        public string NhomHang { get; set; } = "Dịch vụ";

        [StringLength(30)]
        [Display(Name = "Đơn vị tính")]
        public string DonViTinh { get; set; } = "Lần";

        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        [Display(Name = "Giá vốn")]
        public decimal GiaVon { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Tồn kho")]
        public int TonKho { get; set; }

        [StringLength(500)]
        [Display(Name = "Ảnh hàng hóa")]
        public string? AnhUrl { get; set; }

        [Display(Name = "Yêu thích")]
        public bool YeuThich { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ")]
        [StringLength(100, ErrorMessage = "Tên dịch vụ không được vượt quá 100 ký tự")]
        [Display(Name = "Tên dịch vụ")]
        public string TenDichVu { get; set; }

        [Display(Name = "Mô tả")]
        [DataType(DataType.MultilineText)]
        public string MoTa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá dịch vụ")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá dịch vụ phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Giá (VNĐ)")]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = false)]
        public decimal Gia { get; set; }

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;
    }
}
