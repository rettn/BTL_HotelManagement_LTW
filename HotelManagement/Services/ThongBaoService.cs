using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public sealed class ThongBaoService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ThongBaoService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task TaoDatPhongMoiChoNhanVienAsync(DatPhong datPhong)
    {
        var settings = await _context.CaiDatHeThongs.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ThongBaoDatMoi != true || !KenhHeThongBat(settings.KenhThongBao)) return;

        var customerName = await _context.KhachHangs.Where(k => k.MaKH == datPhong.MaKH)
            .Select(k => k.HoTen).FirstOrDefaultAsync() ?? "Khách hàng";
        var roomCount = await _context.ChiTietDatPhongs.CountAsync(c => c.MaDatPhong == datPhong.MaDatPhong);
        await TaoChoNhanVienAsync(
            $"Đặt phòng mới DP{datPhong.MaDatPhong:D6}",
            $"{customerName} vừa tạo đặt phòng gồm {roomCount} phòng.",
            $"DatPhong:{datPhong.MaDatPhong}");
    }

    public async Task LamMoiCanhBaoVanHanhAsync(string userId)
    {
        var settings = await _context.CaiDatHeThongs.AsNoTracking().FirstOrDefaultAsync();
        if (settings == null || !KenhHeThongBat(settings.KenhThongBao)) return;

        if (settings.ThongBaoSapNhanTra)
            await TaoThongBaoLichPhongHomNayAsync(userId);
        if (settings.ThongBaoKho)
            await DongBoCanhBaoTonKhoAsync(userId);
    }

    private async Task TaoThongBaoLichPhongHomNayAsync(string userId)
    {
        var today = DateTime.Today;
        var stays = await _context.ChiTietDatPhongs.AsNoTracking()
            .Include(c => c.DatPhong).Include(c => c.Phong)
            .Where(c => c.DatPhong != null && c.DatPhong.TrangThai != "DaHuy"
                && (c.NgayNhan.Date == today || c.NgayTra.Date == today))
            .ToListAsync();

        var existingTypes = await _context.ThongBaos.AsNoTracking()
            .Where(n => n.MaNguoiDung == userId && n.NgayTao.Date == today)
            .Select(n => n.LoaiThongBao).ToListAsync();

        foreach (var detail in stays)
        {
            var room = detail.Phong?.SoPhong ?? detail.MaPhong.ToString();
            var bookingCode = $"DP{detail.MaDatPhong:D6}";
            if (detail.NgayNhan.Date == today)
            {
                var type = $"NhanPhong:{detail.MaChiTietDatPhong}:{today:yyyyMMdd}";
                if (!existingTypes.Contains(type))
                    await TaoCaNhanAsync(userId, "Lịch nhận phòng hôm nay", $"Phòng {room} thuộc {bookingCode} dự kiến nhận hôm nay.", type);
            }
            if (detail.NgayTra.Date == today)
            {
                var type = $"TraPhong:{detail.MaChiTietDatPhong}:{today:yyyyMMdd}";
                if (!existingTypes.Contains(type))
                    await TaoCaNhanAsync(userId, "Lịch trả phòng hôm nay", $"Phòng {room} thuộc {bookingCode} dự kiến trả hôm nay.", type);
            }
        }
    }

    private async Task DongBoCanhBaoTonKhoAsync(string userId)
    {
        var lowStock = await _context.DichVus.AsNoTracking()
            .Where(d => d.LoaiHang == "SanPham" && d.TrangThai && d.TonKho <= 10)
            .Select(d => new { d.Id, d.TenDichVu, d.TonKho }).ToListAsync();
        var notifications = await _context.ThongBaos
            .Where(n => n.MaNguoiDung == userId && (n.LoaiThongBao.StartsWith("SapHetKho:") || n.LoaiThongBao.StartsWith("KhoDu:")))
            .OrderByDescending(n => n.NgayTao).ToListAsync();
        var latestByProduct = notifications
            .Select(n => new { Notification = n, Parts = n.LoaiThongBao.Split(':') })
            .Where(x => x.Parts.Length >= 2 && int.TryParse(x.Parts[1], out _))
            .GroupBy(x => x.Parts[1])
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Notification.NgayTao).First().Notification);
        var lowIds = lowStock.Select(d => d.Id.ToString()).ToHashSet();

        foreach (var pair in latestByProduct.Where(pair => pair.Value.LoaiThongBao.StartsWith("SapHetKho:")
                     && !lowIds.Contains(pair.Key)))
        {
            pair.Value.DaDoc = true;
            var recoveredType = $"KhoDu:{pair.Key}:{DateTime.Now:yyMMddHHmmss}";
            if (!notifications.Any(n => n.LoaiThongBao == recoveredType))
                _context.ThongBaos.Add(new ThongBao
                {
                    MaNguoiDung = userId,
                    TieuDe = "Tồn kho đã được bổ sung",
                    NoiDung = "Sản phẩm đã ra khỏi ngưỡng cảnh báo tồn kho thấp.",
                    LoaiThongBao = recoveredType,
                    NgayTao = DateTime.Now,
                    DaDoc = true
                });
        }

        foreach (var item in lowStock)
        {
            var id = item.Id.ToString();
            if (latestByProduct.TryGetValue(id, out var latest) && latest.LoaiThongBao.StartsWith("SapHetKho:")) continue;
            var title = item.TonKho == 0 ? "Sản phẩm đã hết hàng" : "Sản phẩm sắp hết hàng";
            await TaoCaNhanAsync(userId, title, $"{item.TenDichVu} còn {item.TonKho} sản phẩm trong kho.", $"SapHetKho:{item.Id}");
        }

        if (_context.ChangeTracker.HasChanges()) await _context.SaveChangesAsync();
    }

    private async Task TaoChoNhanVienAsync(string title, string body, string type)
    {
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        var staff = await _userManager.GetUsersInRoleAsync("NhanVien");
        var recipients = admins.Concat(staff).Select(u => u.Id).Distinct().ToList();
        if (recipients.Count == 0) return;
        _context.ThongBaos.AddRange(recipients.Select(id => new ThongBao
        {
            MaNguoiDung = id, TieuDe = title, NoiDung = body, LoaiThongBao = type, NgayTao = DateTime.Now
        }));
        await _context.SaveChangesAsync();
    }

    private async Task TaoCaNhanAsync(string userId, string title, string body, string type)
    {
        _context.ThongBaos.Add(new ThongBao
        {
            MaNguoiDung = userId, TieuDe = title, NoiDung = body, LoaiThongBao = type, NgayTao = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }

    public static bool KenhHeThongBat(string? channels) =>
        (channels ?? "System").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Contains("System", StringComparer.OrdinalIgnoreCase);
}
