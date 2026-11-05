using HotelManagement.Data;
using HotelManagement.Extensions;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers.Api.V1;

[ApiController]
[Authorize(Roles = "Admin,NhanVien")]
[Route("api/v1/rooms")]
public sealed class RoomsApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiPage<RoomApiDto>>> Get([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = context.Phongs.AsNoTracking().Include(x => x.LoaiPhong).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.TrangThai == status);
        var result = await query.OrderBy(x => x.SoPhong).ToPagedResultAsync(page, pageSize);
        return Ok(new ApiPage<RoomApiDto>(result.Items.Select(x => new RoomApiDto(
            x.MaPhong, x.SoPhong, x.TrangThai, x.MaLoaiPhong, x.LoaiPhong!.TenLoaiPhong,
            x.LoaiPhong.GiaTheoDem, x.LoaiPhong.SoNguoiToiDa)).ToList(), result.PageIndex,
            result.PageSize, result.TotalItems, (int)Math.Ceiling(result.TotalItems / (double)result.PageSize)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomApiDto>> GetById(int id)
    {
        var room = await context.Phongs.AsNoTracking().Include(x => x.LoaiPhong).FirstOrDefaultAsync(x => x.MaPhong == id);
        if (room is null) return NotFound();
        return Ok(new RoomApiDto(room.MaPhong, room.SoPhong, room.TrangThai, room.MaLoaiPhong,
            room.LoaiPhong!.TenLoaiPhong, room.LoaiPhong.GiaTheoDem, room.LoaiPhong.SoNguoiToiDa));
    }
}
