namespace HotelManagement.ViewModels;

public class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }

    public PaginationViewModel ToPagination(string action, string? controller, Dictionary<string, string?>? routeValues = null) => new()
    {
        PageIndex = PageIndex,
        PageSize = PageSize,
        TotalItems = TotalItems,
        Action = action,
        Controller = controller,
        RouteValues = routeValues ?? new()
    };
}
