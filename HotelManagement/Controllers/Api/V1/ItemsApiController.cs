using HotelManagement.Data;
using HotelManagement.Extensions;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers.Api.V1;

[ApiController]
[Authorize(Roles = "Admin,NhanVien")]
[Route("api/v1/items")]
public sealed class ItemsApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiPage<ItemApiDto>>> Get(
        [FromQuery] string? keyword, [FromQuery] string? type, [FromQuery] bool? active,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = context.DichVus.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(x => x.TenDichVu.Contains(term) || x.MaHang.Contains(term));
        }
        if (type is "DichVu" or "SanPham") query = query.Where(x => x.LoaiHang == type);
        if (active.HasValue) query = query.Where(x => x.TrangThai == active.Value);
        var result = await query.OrderBy(x => x.TenDichVu).ToPagedResultAsync(page, pageSize);
        return Ok(new ApiPage<ItemApiDto>(result.Items.Select(x => new ItemApiDto(
            x.Id, x.MaHang, x.TenDichVu, x.LoaiHang, x.NhomHang, x.Gia,
            x.LoaiHang == "SanPham" ? x.TonKho : null, x.TrangThai, x.AnhUrl)).ToList(),
            result.PageIndex, result.PageSize, result.TotalItems,
            (int)Math.Ceiling(result.TotalItems / (double)result.PageSize)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemApiDto>> GetById(int id)
    {
        var x = await context.DichVus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return x is null ? NotFound() : Ok(new ItemApiDto(x.Id, x.MaHang, x.TenDichVu,
            x.LoaiHang, x.NhomHang, x.Gia, x.LoaiHang == "SanPham" ? x.TonKho : null,
            x.TrangThai, x.AnhUrl));
    }
}
