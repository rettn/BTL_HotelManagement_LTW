namespace HotelManagement.ViewModels
{
    public class HuyPhongRequestModel
    {
        public int MaDatPhong { get; set; }
        public decimal SoTienHoan { get; set; }
        public double PhanTramHoan { get; set; }
        public string HinhThucHoan { get; set; } = "TienMat";
        public string LyDoHuy { get; set; } = string.Empty;
    }
}

