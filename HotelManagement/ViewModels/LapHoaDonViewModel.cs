
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.ViewModels
{
    public class LapHoaDonViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn lượt lưu trú.")]
        public int MaLuuTru { get; set; }

        public int MaDatPhong { get; set; }
        public int MaChiTietDatPhong { get; set; }

        public string TenKhachHang { get; set; } = string.Empty;
        public string SoPhong { get; set; } = string.Empty;

        public DateTime ThoiGianNhanPhong { get; set; }
        public DateTime ThoiGianTraPhong { get; set; }

        public int SoDem { get; set; }
        public decimal DonGiaPhong { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienDichVu { get; set; }

        [Range(typeof(decimal), "0", "999999999999",
            ErrorMessage = "Giảm giá không được âm.")]
        public decimal GiamGia { get; set; }

        public decimal TongTien => TienPhong + TienDichVu - GiamGia;

        public List<SelectListItem> DanhSachLuuTru { get; set; } = new();
    }
}
