using System.Security.Claims;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers;

[Authorize(Roles = "Admin,NhanVien")]
public sealed class ThongBaoController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ThongBaoService _service;

    public ThongBaoController(ApplicationDbContext context, ThongBaoService service)
    {
        _context = context;
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        await _service.LamMoiCanhBaoVanHanhAsync(userId);
        var notifications = await _context.ThongBaos.AsNoTracking()
            .Where(n => n.MaNguoiDung == userId && !n.LoaiThongBao.StartsWith("KhoDu:"))
            .OrderByDescending(n => n.NgayTao)
            .Take(100)
            .ToListAsync();
        ViewBag.UnreadCount = notifications.Count(n => !n.DaDoc);
        return View(notifications.Select(ToItem).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Recent()
    {
        var userId = CurrentUserId();
        await _service.LamMoiCanhBaoVanHanhAsync(userId);
        var query = _context.ThongBaos.AsNoTracking()
            .Where(n => n.MaNguoiDung == userId && !n.LoaiThongBao.StartsWith("KhoDu:"));
        var unread = await query.CountAsync(n => !n.DaDoc);
        var items = await query.OrderByDescending(n => n.NgayTao).Take(8).ToListAsync();
        return Json(new { unreadCount = unread, items = items.Select(ToItem) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, string? returnUrl)
    {
        var notification = await _context.ThongBaos.FirstOrDefaultAsync(n =>
            n.MaThongBao == id && n.MaNguoiDung == CurrentUserId());
        if (notification == null) return NotFound(new { success = false });
        notification.DaDoc = true;
        await _context.SaveChangesAsync();
        return WantsJson() ? Json(new { success = true })
            : Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = CurrentUserId();
        await _context.ThongBaos.Where(n => n.MaNguoiDung == userId && !n.DaDoc)
            .ExecuteUpdateAsync(update => update.SetProperty(n => n.DaDoc, true));
        return WantsJson() ? Json(new { success = true }) : RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearRead()
    {
        var userId = CurrentUserId();
        await _context.ThongBaos.Where(n => n.MaNguoiDung == userId && n.DaDoc
                && !n.LoaiThongBao.StartsWith("KhoDu:"))
            .ExecuteDeleteAsync();
        TempData["Success"] = "Đã xóa các thông báo đã đọc.";
        return RedirectToAction(nameof(Index));
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool WantsJson() => Request.Headers.Accept.Any(value =>
        value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

    private ThongBaoItemViewModel ToItem(ThongBao n)
    {
        var parts = n.LoaiThongBao.Split(':');
        var controller = "DatPhong";
        var action = "Index";
        var routeId = 0;
        if (parts.Length > 1) int.TryParse(parts[1], out routeId);

        if (parts[0] == "SapHetKho")
        {
            controller = "Kho";
        }
        else if (parts[0] == "DatPhong")
        {
            controller = "DatPhong";
            action = "Details";
        }
        else if (parts[0] is "NhanPhong" or "TraPhong")
        {
            controller = "DatPhong";
            action = "Details";
            routeId = GetBookingIdFromBody(n.NoiDung);
        }

        var url = controller == "Kho"
            ? Url.Action("Index", "Kho", new { tonKho = n.NoiDung.Contains("còn 0 sản phẩm", StringComparison.OrdinalIgnoreCase) ? "out" : "low" })
            : action == "Details" && routeId > 0
                ? Url.Action(action, controller, new { id = routeId })
                : Url.Action("Index", controller);

        return new ThongBaoItemViewModel
        {
            Id = n.MaThongBao,
            TieuDe = n.TieuDe,
            NoiDung = n.NoiDung,
            NgayTao = n.NgayTao,
            DaDoc = n.DaDoc,
            LoaiThongBao = n.LoaiThongBao,
            Url = url ?? Url.Action("Index", "ThongBao") ?? "/ThongBao"
        };
    }

    private static int GetBookingIdFromBody(string body)
    {
        var marker = body.IndexOf("DP", StringComparison.OrdinalIgnoreCase);
        return marker >= 0 && marker + 8 <= body.Length
            ? int.TryParse(body.Substring(marker + 2, 6), out var id) ? id : 0
            : 0;
    }
}
