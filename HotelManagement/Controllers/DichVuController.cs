using HotelManagement.Data;
using HotelManagement.Extensions;
using HotelManagement.Models;
using HotelManagement.Security;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers;

[Authorize(Roles = "Admin,NhanVien")]
public class DichVuController : Controller
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public DichVuController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string? keyword, string? loaiHang, string? nhomHang, string? tonKho, string? trangThai, int page = 1, int pageSize = 10)
    {
        var query = _context.DichVus.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(d => d.TenDichVu.Contains(keyword) || d.MaHang.Contains(keyword));
        if (!string.IsNullOrWhiteSpace(loaiHang))
            query = query.Where(d => d.LoaiHang == loaiHang);
        if (!string.IsNullOrWhiteSpace(nhomHang))
            query = query.Where(d => d.NhomHang == nhomHang);
        if (trangThai == "active") query = query.Where(d => d.TrangThai);
        if (trangThai == "inactive") query = query.Where(d => !d.TrangThai);
        if (tonKho == "out") query = query.Where(d => d.LoaiHang == "SanPham" && d.TonKho == 0);
        if (tonKho == "low") query = query.Where(d => d.LoaiHang == "SanPham" && d.TonKho > 0 && d.TonKho <= 10);

        ViewBag.Keyword = keyword;
        ViewBag.LoaiHang = loaiHang;
        ViewBag.NhomHang = nhomHang;
        ViewBag.TonKho = tonKho;
        ViewBag.TrangThai = trangThai;
        ViewBag.NhomHangs = await _context.DichVus.AsNoTracking()
            .Where(d => d.NhomHang != null && d.NhomHang != "")
            .Select(d => d.NhomHang).Distinct().OrderBy(x => x).ToListAsync();
        var result = await query.OrderBy(d => d.LoaiHang).ThenBy(d => d.TenDichVu).ToPagedResultAsync(page, pageSize);
        ViewBag.Pagination = result.ToPagination(nameof(Index), "DichVu", new Dictionary<string, string?>
        {
            ["keyword"] = keyword, ["loaiHang"] = loaiHang, ["nhomHang"] = nhomHang,
            ["tonKho"] = tonKho, ["trangThai"] = trangThai
        });
        return View(result.Items);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var item = await _context.DichVus.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Create(string? loaiHang = null)
    {
        ViewBag.NhomHangs = await GetGroups();
        return View(new DichVuFormViewModel
        {
            LoaiHang = loaiHang == "SanPham" ? "SanPham" : "DichVu",
            NhomHang = loaiHang == "SanPham" ? "Đồ ăn" : "Dịch vụ",
            DonViTinh = loaiHang == "SanPham" ? "Gói" : "Lần"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DichVuFormViewModel model)
    {
        Normalize(model);
        string? uploadedImage = null;
        if (ModelState.IsValid && model.Anh is not null)
        {
            var result = await SaveImageAsync(model.Anh);
            if (result.Error is not null) ModelState.AddModelError(nameof(model.Anh), result.Error);
            else uploadedImage = result.Url;
        }

        if (!ModelState.IsValid)
        {
            model.AnhUrl = null;
            ViewBag.NhomHangs = await GetGroups();
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var item = new DichVu
            {
                TenDichVu = model.TenDichVu.Trim(),
                MoTa = model.MoTa?.Trim() ?? string.Empty,
                Gia = model.Gia,
                GiaVon = model.GiaVon,
                LoaiHang = model.LoaiHang,
                NhomHang = model.NhomHang,
                DonViTinh = model.DonViTinh,
                TonKho = model.LoaiHang == "SanPham" ? model.TonKho : 0,
                TrangThai = model.TrangThai,
                YeuThich = model.YeuThich,
                AnhUrl = uploadedImage
            };
            _context.DichVus.Add(item);
            await _context.SaveChangesAsync();
            item.MaHang = $"{(item.LoaiHang == "SanPham" ? "SP" : "DV")}{item.Id:D6}";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Đã thêm hàng hóa.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            await transaction.RollbackAsync();
            if (uploadedImage is not null) DeleteImage(uploadedImage);
            ModelState.AddModelError(string.Empty, "Không thể lưu hàng hóa. Vui lòng thử lại.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var item = await _context.DichVus.FindAsync(id);
        ViewBag.NhomHangs = await GetGroups();
        return item == null ? NotFound() : View(DichVuFormViewModel.From(item));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DichVuFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        Normalize(model);
        var item = await _context.DichVus.FirstOrDefaultAsync(d => d.Id == id);
        if (item == null) return NotFound();
        model.AnhUrl = item.AnhUrl;

        string? uploadedImage = null;
        if (ModelState.IsValid && model.Anh is not null)
        {
            var result = await SaveImageAsync(model.Anh);
            if (result.Error is not null) ModelState.AddModelError(nameof(model.Anh), result.Error);
            else uploadedImage = result.Url;
        }
        if (!ModelState.IsValid) { ViewBag.NhomHangs = await GetGroups(); return View(model); }

        var oldImage = item.AnhUrl;
        item.TenDichVu = model.TenDichVu.Trim();
        item.MoTa = model.MoTa?.Trim() ?? string.Empty;
        item.Gia = model.Gia;
        item.GiaVon = model.GiaVon;
        item.LoaiHang = model.LoaiHang;
        item.NhomHang = model.NhomHang;
        item.DonViTinh = model.DonViTinh;
        if (item.LoaiHang == "SanPham") item.TonKho = model.TonKho;
        item.TrangThai = model.TrangThai;
        item.YeuThich = model.YeuThich;
        if (uploadedImage is not null) item.AnhUrl = uploadedImage;

        try
        {
            await _context.SaveChangesAsync();
            if (uploadedImage is not null && oldImage is not null) DeleteImage(oldImage);
            TempData["SuccessMessage"] = "Đã cập nhật hàng hóa.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (uploadedImage is not null) DeleteImage(uploadedImage);
            if (!await _context.DichVus.AnyAsync(d => d.Id == id)) return NotFound();
            throw;
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var item = await _context.DichVus.FindAsync(id);
        if (item == null) return NotFound();
        item.TrangThai = !item.TrangThai;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(int id)
    {
        var item = await _context.DichVus.FindAsync(id);
        if (item == null) return NotFound();
        item.YeuThich = !item.YeuThich;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var item = await _context.DichVus.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _context.DichVus.FindAsync(id);
        if (item == null) return RedirectToAction(nameof(Index));
        if (await _context.ChiTietDichVus.AnyAsync(x => x.MaDichVu == id)
            || await _context.ChiTietNhapKhos.AnyAsync(x => x.DichVuId == id))
        {
            item.TrangThai = false;
            TempData["ErrorMessage"] = "Hàng hóa đã phát sinh sử dụng nên được chuyển sang ngừng kinh doanh thay vì xóa.";
        }
        else
        {
            _context.DichVus.Remove(item);
            if (item.AnhUrl is not null) DeleteImage(item.AnhUrl);
            TempData["SuccessMessage"] = "Đã xóa hàng hóa.";
        }
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private static void Normalize(DichVuFormViewModel model)
    {
        model.MoTa = RichTextSanitizer.Sanitize(model.MoTa);
        model.LoaiHang = model.LoaiHang == "SanPham" ? "SanPham" : "DichVu";
        model.NhomHang = string.IsNullOrWhiteSpace(model.NhomHang)
            ? (model.LoaiHang == "SanPham" ? "Hàng hóa" : "Dịch vụ")
            : model.NhomHang.Trim();
        model.DonViTinh = string.IsNullOrWhiteSpace(model.DonViTinh)
            ? (model.LoaiHang == "SanPham" ? "Cái" : "Lần")
            : model.DonViTinh.Trim();
    }

    private Task<List<string>> GetGroups() => _context.DichVus.AsNoTracking().Where(d => d.NhomHang != null && d.NhomHang != "").Select(d => d.NhomHang).Distinct().OrderBy(x => x).ToListAsync();

    private async Task<(string? Url, string? Error)> SaveImageAsync(IFormFile image)
    {
        var extension = Path.GetExtension(image.FileName);
        if (!ImageExtensions.Contains(extension)) return (null, "Chỉ nhận ảnh JPG, PNG hoặc WEBP.");
        if (image.Length == 0 || image.Length > MaxImageBytes) return (null, "Ảnh phải có dung lượng nhỏ hơn 5 MB.");

        var folder = Path.Combine(_environment.WebRootPath, "uploads", "hang-hoa");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(folder, fileName);
        await using (var stream = System.IO.File.Create(filePath))
            await image.CopyToAsync(stream);
        return ($"/uploads/hang-hoa/{fileName}", null);
    }

    private void DeleteImage(string url)
    {
        const string prefix = "/uploads/hang-hoa/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;
        var fileName = Path.GetFileName(url[prefix.Length..]);
        var path = Path.Combine(_environment.WebRootPath, "uploads", "hang-hoa", fileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
}
