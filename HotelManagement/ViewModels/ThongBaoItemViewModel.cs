namespace HotelManagement.ViewModels;

public sealed class ThongBaoItemViewModel
{
    public int Id { get; init; }
    public string TieuDe { get; init; } = "";
    public string NoiDung { get; init; } = "";
    public DateTime NgayTao { get; init; }
    public bool DaDoc { get; init; }
    public string LoaiThongBao { get; init; } = "";
    public string Url { get; init; } = "#";
}
