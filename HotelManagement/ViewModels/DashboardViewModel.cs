using HotelManagement.Models;

namespace HotelManagement.ViewModels
{
    public class DashboardViewModel
    {
        public int TongSoPhong { get; set; }
        public int PhongDangSuDung { get; set; }
        public int PhieuChoXacNhan { get; set; }
        public decimal DoanhThuHomNay { get; set; }

        public List<string> LabelsTuan { get; set; } = new() { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
        public List<decimal> DoanhThuTuan { get; set; } = new();

        public int SoPhongTrong { get; set; }
        public int SoPhongDangDon { get; set; }
        public int SoPhongDangSuDung { get; set; }
        public int SoPhongDaDat { get; set; }

        public List<DatPhong> DatPhongGanDay { get; set; } = new();
    }
}