using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers;

[Authorize(Roles = "Admin")]
public class PhuCapController : Controller
{
    private readonly PhuCapService _service;
    private readonly NhanVienService _nhanVienService;
    public PhuCapController(PhuCapService service, NhanVienService nhanVienService) { _service = service; _nhanVienService = nhanVienService; }
    private async Task LoadEmployeesAsync() => ViewBag.NhanViens = await _nhanVienService.GetAllAsync();
    public async Task<IActionResult> Index(int? thang, int? nam)
    {
        var t = thang ?? DateTime.Today.Month; var n = nam ?? DateTime.Today.Year;
        if (t < 1 || t > 12 || n < 2000 || n > 2100) return BadRequest("Tháng hoặc năm không hợp lệ.");
        ViewBag.Thang = t; ViewBag.Nam = n;
        var rows = await _service.GetByMonthAsync(t, n); ViewBag.TongTien = rows.Sum(x => x.SoTien); return View(rows);
    }
    [HttpGet] public async Task<IActionResult> Create(int? thang, int? nam) { await LoadEmployeesAsync(); return View(new PhuCap { Thang = thang ?? DateTime.Today.Month, Nam = nam ?? DateTime.Today.Year }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Create(PhuCap model)
    { if (!ModelState.IsValid) { await LoadEmployeesAsync(); return View(model); } var r = await _service.AddAsync(model); if (r.Success) { TempData["Success"] = "Thêm khoản phụ cấp thành công."; return RedirectToAction(nameof(Index), new { thang = model.Thang, nam = model.Nam }); } ModelState.AddModelError("", r.Message); await LoadEmployeesAsync(); return View(model); }
    [HttpGet] public async Task<IActionResult> Edit(int id) { var m = await _service.GetByIdAsync(id); if (m == null) return NotFound(); await LoadEmployeesAsync(); return View(m); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Edit(int id, PhuCap model)
    { if (id != model.MaPhuCap) return BadRequest(); if (!ModelState.IsValid) { await LoadEmployeesAsync(); return View(model); } var r = await _service.UpdateAsync(model); if (r.Success) { TempData["Success"] = "Cập nhật khoản phụ cấp thành công."; return RedirectToAction(nameof(Index), new { thang = model.Thang, nam = model.Nam }); } ModelState.AddModelError("", r.Message); await LoadEmployeesAsync(); return View(model); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Delete(int id)
    { var m = await _service.GetByIdAsync(id); if (m == null) return NotFound(); var t = m.Thang; var n = m.Nam; var r = await _service.DeleteAsync(id); TempData[r.Success ? "Success" : "Error"] = r.Message; return RedirectToAction(nameof(Index), new { thang = t, nam = n }); }
}
