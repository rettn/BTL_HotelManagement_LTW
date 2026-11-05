using HotelManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Extensions;

public static class PaginationExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize)
    {
        pageSize = pageSize is 10 or 20 or 50 ? pageSize : 10;
        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, pages);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<T> { Items = items, PageIndex = page, PageSize = pageSize, TotalItems = total };
    }
}
