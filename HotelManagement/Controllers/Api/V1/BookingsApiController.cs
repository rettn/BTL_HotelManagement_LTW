using HotelManagement.Data;
using HotelManagement.Extensions;
using HotelManagement.Models;
using HotelManagement.Services;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers.Api.V1;

[ApiController]
[Authorize(Roles = "Admin,NhanVien")]
[Route("api/v1/bookings")]
public sealed class BookingsApiController(ApplicationDbContext context, DatPhongService bookingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiPage<BookingApiDto>>> Get(
        [FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = context.DatPhongs.AsNoTracking().Include(x => x.KhachHang)
            .Include(x => x.ChiTietDatPhongs).ThenInclude(x => x.Phong).ThenInclude(x => x!.LoaiPhong)
            .Include(x => x.ChiTietDatPhongs).ThenInclude(x => x.DanhSachLuuTru).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.TrangThai == status);
        if (from.HasValue) query = query.Where(x => x.NgayNhanDuKien >= from.Value.Date);
        if (to.HasValue) query = query.Where(x => x.NgayNhanDuKien < to.Value.Date.AddDays(1));
        var result = await query.OrderByDescending(x => x.MaDatPhong).ToPagedResultAsync(page, pageSize);
        var items = result.Items.Select(Map).ToList();
        return Ok(new ApiPage<BookingApiDto>(items, result.PageIndex, result.PageSize,
            result.TotalItems, (int)Math.Ceiling(result.TotalItems / (double)result.PageSize)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingApiDto>> GetById(int id)
    {
        var booking = await Query().FirstOrDefaultAsync(x => x.MaDatPhong == id);
        return booking is null ? NotFound() : Ok(Map(booking));
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateBookingApiRequest request)
    {
        var input = new DatPhongViewModel
        {
            MaKH = request.GuestId, NgayNhan = request.CheckIn, NgayTra = request.CheckOut,
            TienCoc = request.Deposit, MaPhongs = request.RoomIds, GhiChu = request.Note
        };
        var result = await bookingService.CreateAsync(input);
        if (!result.Success)
            return Conflict(new ProblemDetails { Title = "Không thể tạo đặt phòng", Detail = result.Message, Status = StatusCodes.Status409Conflict });
        var created = await Query().FirstAsync(x => x.MaDatPhong == result.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, Map(created));
    }

    [HttpPost("{id:int}/rooms/{roomId:int}/check-in")]
    public async Task<IActionResult> CheckIn(int id, int roomId)
    {
        var result = await bookingService.CheckInAsync(id, roomId);
        return result.Success ? Ok(new { message = result.Message }) : Conflict(new ProblemDetails
        {
            Title = "Không thể nhận phòng", Detail = result.Message, Status = StatusCodes.Status409Conflict
        });
    }

    [HttpPost("{id:int}/rooms/{roomId:int}/check-out")]
    public async Task<IActionResult> CheckOut(int id, int roomId)
    {
        var result = await bookingService.CheckOutAsync(id, roomId);
        return result.Success ? Ok(new { message = result.Message }) : Conflict(new ProblemDetails
        {
            Title = "Không thể trả phòng", Detail = result.Message, Status = StatusCodes.Status409Conflict
        });
    }

    private IQueryable<DatPhong> Query() => context.DatPhongs.AsNoTracking().Include(x => x.KhachHang)
        .Include(x => x.ChiTietDatPhongs).ThenInclude(x => x.Phong).ThenInclude(x => x!.LoaiPhong)
        .Include(x => x.ChiTietDatPhongs).ThenInclude(x => x.DanhSachLuuTru);

    private static BookingApiDto Map(DatPhong x) => new(
        x.MaDatPhong, $"DP{x.MaDatPhong:D6}", x.TrangThai, x.NgayDat,
        x.NgayNhanDuKien, x.NgayTraDuKien, x.TienCoc, x.MaKH,
        x.KhachHang?.HoTen ?? string.Empty, x.KhachHang?.SoDienThoai,
        x.ChiTietDatPhongs.Select(detail => new BookingRoomApiDto(
            detail.MaChiTietDatPhong, detail.MaPhong, detail.Phong?.SoPhong ?? string.Empty,
            detail.Phong?.LoaiPhong?.TenLoaiPhong ?? string.Empty, detail.NgayNhan, detail.NgayTra,
            detail.DonGia, detail.DanhSachLuuTru.FirstOrDefault()?.ThoiGianNhanPhong,
            detail.DanhSachLuuTru.FirstOrDefault()?.ThoiGianTraPhong)).ToList());
}
