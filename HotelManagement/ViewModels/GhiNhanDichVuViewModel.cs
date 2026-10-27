using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels
{
    public class GhiNhanDichVuViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn lượt lưu trú.")]
        public int MaLuuTru { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn dịch vụ.")]
        public int MaDichVu { get; set; }

        [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 đến 1000.")]
        public int SoLuong { get; set; } = 1;

        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
        public string? GhiChu { get; set; }

        public List<SelectListItem> DanhSachLuuTru { get; set; }
            = new();

        public List<SelectListItem> DanhSachDichVu { get; set; }
            = new();
    }
}
