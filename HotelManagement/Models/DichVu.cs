using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class DichVu
    {
        [Key]
        public int Id { get; set; }

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