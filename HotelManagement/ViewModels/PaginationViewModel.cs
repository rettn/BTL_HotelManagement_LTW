namespace HotelManagement.ViewModels;

public class PaginationViewModel
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public string Action { get; set; } = "Index";
    public string? Controller { get; set; }
    public Dictionary<string, string?> RouteValues { get; set; } = new();

    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    public int FirstItem => TotalItems == 0 ? 0 : (PageIndex - 1) * PageSize + 1;
    public int LastItem => Math.Min(PageIndex * PageSize, TotalItems);
    public bool HasPrevious => PageIndex > 1;
    public bool HasNext => PageIndex < TotalPages;
}
