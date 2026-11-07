namespace HotelManagement.ViewModels;

public class QuanTriQuyenViewModel
{
    public List<string> QuyenDaChon { get; set; } = [];
    public List<QuyenChucNangViewModel> ChucNangs { get; set; } = [];
}

public class QuyenChucNangViewModel
{
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public string MoTa { get; set; } = "";
    public bool DuocCap { get; set; }
}
