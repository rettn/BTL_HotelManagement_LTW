using System.Globalization;
using System.Text;
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public sealed class ChatbotContextService
{
    private readonly ApplicationDbContext _db;

    public ChatbotContextService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> BuildCustomerContextAsync(DateTime? checkIn, DateTime? checkOut, CancellationToken cancellationToken)
    {
        var settings = await _db.CaiDatHeThongs.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var roomTypes = await _db.LoaiPhongs.AsNoTracking().OrderBy(x => x.TenLoaiPhong).ToListAsync(cancellationToken);
        var rooms = await _db.Phongs.AsNoTracking().Include(x => x.LoaiPhong).ToListAsync(cancellationToken);

        var context = new StringBuilder();
        context.AppendLine("THÔNG TIN CÔNG KHAI CỦA KHÁCH SẠN:");
        context.AppendLine($"Tên: {settings?.TenKhachSan ?? "Hotel Management"}");
        context.AppendLine($"Địa chỉ: {settings?.DiaChi ?? "Chưa cập nhật"}");
        context.AppendLine($"Điện thoại: {settings?.SoDienThoai ?? "Chưa cập nhật"}");
        context.AppendLine($"Email: {settings?.Email ?? "Chưa cập nhật"}");
        context.AppendLine($"Giờ nhận phòng: {settings?.GioNhanPhong ?? "Chưa cập nhật"}; giờ trả phòng: {settings?.GioTraPhong ?? "Chưa cập nhật"}");
        context.AppendLine($"Chính sách hủy: {settings?.ChinhSachHuy ?? "Vui lòng liên hệ khách sạn"}");
        context.AppendLine("LOẠI PHÒNG:");
        foreach (var type in roomTypes)
            context.AppendLine($"- {type.TenLoaiPhong}: tối đa {type.SoNguoiToiDa} khách, {type.GiaTheoDem.ToString("N0", CultureInfo.InvariantCulture)} VNĐ/đêm, mô tả: {type.MoTa ?? "Không có"}");

        if (checkIn is not null && checkOut is not null && checkOut > checkIn)
        {
            var occupiedRoomIds = await _db.ChiTietDatPhongs.AsNoTracking()
                .Where(x => x.NgayNhan < checkOut && x.NgayTra > checkIn && x.DatPhong != null && x.DatPhong.TrangThai != "DaHuy")
                .Select(x => x.MaPhong)
                .Distinct()
                .ToListAsync(cancellationToken);
            context.AppendLine($"TÌNH TRẠNG PHÒNG từ {checkIn:dd/MM/yyyy} đến {checkOut:dd/MM/yyyy}:");
            foreach (var group in rooms.GroupBy(x => x.LoaiPhong?.TenLoaiPhong ?? "Chưa phân loại"))
            {
                var available = group.Count(x => !occupiedRoomIds.Contains(x.MaPhong) && IsAvailableStatus(x.TrangThai));
                context.AppendLine($"- {group.Key}: còn {available}/{group.Count()} phòng có thể đặt");
            }
        }
        else
        {
            context.AppendLine("TÌNH TRẠNG HIỆN TẠI:");
            foreach (var group in rooms.GroupBy(x => x.LoaiPhong?.TenLoaiPhong ?? "Chưa phân loại"))
                context.AppendLine($"- {group.Key}: {group.Count(IsAvailableNow)}/{group.Count()} phòng đang ở trạng thái có thể đặt");
        }

        return context.ToString();
    }

    public async Task<string> BuildAdminContextAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var totalRooms = await _db.Phongs.AsNoTracking().CountAsync(cancellationToken);
        var roomsByStatus = await _db.Phongs.AsNoTracking().GroupBy(x => x.TrangThai).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var bookingsByStatus = await _db.DatPhongs.AsNoTracking().GroupBy(x => x.TrangThai).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var monthlyRevenue = await _db.HoaDons.AsNoTracking().Where(x => x.NgayLap >= monthStart && x.TrangThai != "DaHuy").SumAsync(x => (decimal?)x.TongTien, cancellationToken) ?? 0;
        var pendingBookings = await _db.DatPhongs.AsNoTracking().CountAsync(x => x.TrangThai == "ChoXacNhan", cancellationToken);
        var lowStock = await _db.DichVus.AsNoTracking().CountAsync(x => x.LoaiHang == "SanPham" && x.TrangThai && x.TonKho <= 10, cancellationToken);
        var recentBookings = await _db.DatPhongs.AsNoTracking().Include(x => x.KhachHang).OrderByDescending(x => x.NgayDat).Take(10).Select(x => new { x.MaDatPhong, x.NgayDat, x.TrangThai, Customer = x.KhachHang!.HoTen }).ToListAsync(cancellationToken);

        var context = new StringBuilder();
        context.AppendLine("DỮ LIỆU VẬN HÀNH NỘI BỘ - CHỈ DÙNG ĐỂ TRẢ LỜI ADMIN/NHÂN VIÊN:");
        context.AppendLine($"Ngày hiện tại: {today:dd/MM/yyyy}; tổng số phòng: {totalRooms}; doanh thu tháng hiện tại: {monthlyRevenue:N0} VNĐ");
        context.AppendLine($"Đặt phòng chờ xác nhận: {pendingBookings}; sản phẩm tồn kho thấp: {lowStock}");
        context.AppendLine("Phòng theo trạng thái: " + string.Join(", ", roomsByStatus.Select(x => $"{x.Status}={x.Count}")));
        context.AppendLine("Đặt phòng theo trạng thái: " + string.Join(", ", bookingsByStatus.Select(x => $"{x.Status}={x.Count}")));
        context.AppendLine("10 đặt phòng gần nhất (mã, ngày, trạng thái, tên khách):");
        foreach (var booking in recentBookings)
            context.AppendLine($"- #{booking.MaDatPhong}, {booking.NgayDat:dd/MM/yyyy}, {booking.TrangThai}, {booking.Customer ?? "Chưa có tên"}");
        return context.ToString();
    }

    private static bool IsAvailableStatus(string? status) => status is "Trong" or "Trống";
    private static bool IsAvailableNow(Phong room) => IsAvailableStatus(room.TrangThai);
}
