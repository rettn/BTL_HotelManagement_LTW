using HotelManagement.Models;

namespace HotelManagement.ViewModels;

public class BangLuongIndexViewModel
{
    public List<BangLuong> BangLuongs { get; set; } = new();
    public List<NhanVien> NhanViens { get; set; } = new();
    public int Thang { get; set; }
    public int Nam { get; set; }
    public int TongNhanVien { get; set; }
    public decimal TongLuong { get; set; }
    public int NgayCongQuyDinh { get; set; } = 26;
}
