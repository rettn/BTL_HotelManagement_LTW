using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers;

[Authorize(Roles = "Admin")]
public class KhoController : Controller
{
    private readonly ApplicationDbContext _context;

    public KhoController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? keyword, string? tonKho = null)
    {
        var products = _context.DichVus.AsNoTracking().Where(x => x.LoaiHang == "SanPham");
        if (!string.IsNullOrWhiteSpace(keyword))
            products = products.Where(x => x.TenDichVu.Contains(keyword) || x.MaHang.Contains(keyword));
        if (tonKho == "out") products = products.Where(x => x.TonKho == 0);
        if (tonKho == "low") products = products.Where(x => x.TonKho > 0 && x.TonKho <= 10);

        ViewBag.Keyword = keyword;
        ViewBag.TonKhoFilter = tonKho;
        ViewBag.ProductCount = await _context.DichVus.CountAsync(x => x.LoaiHang == "SanPham");
        ViewBag.LowCount = await _context.DichVus.CountAsync(x => x.LoaiHang == "SanPham" && x.TonKho > 0 && x.TonKho <= 10);
        ViewBag.OutCount = await _context.DichVus.CountAsync(x => x.LoaiHang == "SanPham" && x.TonKho == 0);
        ViewBag.StockValue = await _context.DichVus.Where(x => x.LoaiHang == "SanPham").SumAsync(x => (decimal?)(x.TonKho * x.GiaVon)) ?? 0m;
        ViewBag.Receipts = await _context.PhieuNhapKhos.AsNoTracking()
            .Include(x => x.ChiTietNhapKhos).ThenInclude(x => x.DichVu)
            .OrderByDescending(x => x.NgayNhap).Take(15).ToListAsync();
        return View(await products.OrderBy(x => x.TonKho).ThenBy(x => x.TenDichVu).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> NhapHang()
    {
        await SetProducts();
        return View(new NhapKhoFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NhapHang(NhapKhoFormViewModel model)
    {
        model.DongHang ??= new();
        var validLines = model.DongHang.Where(x => x.DichVuId > 0 && x.SoLuong > 0 && x.DonGiaNhap >= 0).ToList();
        if (validLines.Count == 0)
            ModelState.AddModelError(nameof(model.DongHang), "Vui lòng chọn ít nhất một sản phẩm để nhập.");

        var quantities = validLines.GroupBy(x => x.DichVuId)
            .ToDictionary(x => x.Key, x => x.Sum(line => line.SoLuong));
        if (quantities.Values.Any(x => x > 100000))
            ModelState.AddModelError(nameof(model.DongHang), "Tổng số lượng một sản phẩm không được vượt quá 100.000.");

        var products = await _context.DichVus
            .Where(x => quantities.Keys.Contains(x.Id) && x.LoaiHang == "SanPham")
            .ToDictionaryAsync(x => x.Id);
        if (products.Count != quantities.Count)
            ModelState.AddModelError(nameof(model.DongHang), "Có mặt hàng không tồn tại hoặc không phải sản phẩm quản lý tồn kho.");

        if (!ModelState.IsValid)
        {
            await SetProducts();
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var receipt = new PhieuNhapKho
            {
                NgayNhap = DateTime.Now,
                NhaCungCap = string.IsNullOrWhiteSpace(model.NhaCungCap) ? null : model.NhaCungCap.Trim(),
                GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim()
            };
            foreach (var line in validLines)
                receipt.TongTien += line.SoLuong * line.DonGiaNhap;

            _context.PhieuNhapKhos.Add(receipt);
            await _context.SaveChangesAsync();
            receipt.MaPhieu = $"PNK{receipt.Id:D6}";

            foreach (var line in validLines)
            {
                var product = products[line.DichVuId];
                product.TonKho = checked(product.TonKho + line.SoLuong);
                product.GiaVon = line.DonGiaNhap;
                _context.ChiTietNhapKhos.Add(new ChiTietNhapKho
                {
                    PhieuNhapKhoId = receipt.Id,
                    DichVuId = product.Id,
                    SoLuong = line.SoLuong,
                    DonGiaNhap = line.DonGiaNhap,
                    ThanhTien = line.SoLuong * line.DonGiaNhap
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Đã nhập hàng thành công · {receipt.MaPhieu}.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Không thể lưu phiếu nhập. Vui lòng kiểm tra dữ liệu và thử lại.");
            await SetProducts();
            return View(model);
        }
    }

    private async Task SetProducts()
    {
        ViewBag.Products = await _context.DichVus.AsNoTracking()
            .Where(x => x.LoaiHang == "SanPham")
            .OrderBy(x => x.TenDichVu).ToListAsync();
    }
}
