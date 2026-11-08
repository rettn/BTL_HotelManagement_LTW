using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers;

[Authorize(Roles = "Admin")]
public sealed class QuanLyCaLamController(ApplicationDbContext context) : Controller
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
        { "ChoXacNhan", "DaXacNhan", "TuChoi" };

    public async Task<IActionResult> Index(DateTime? weekStart, bool dayView = false, string? search = null, string? status = null, int? employeeId = null)
    {
        var selected = (weekStart ?? DateTime.Today).Date;
        var start = dayView ? selected : selected.AddDays(-(((int)selected.DayOfWeek + 6) % 7));
        var end = dayView ? start : start.AddDays(6);
        var employeesQuery = context.NhanViens.AsNoTracking().Where(x => x.TrangThai);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            employeesQuery = employeesQuery.Where(x => x.HoTen.Contains(term) || x.ChucVu.Contains(term) || x.SoDienThoai!.Contains(term));
        }
        if (employeeId.HasValue) employeesQuery = employeesQuery.Where(x => x.MaNV == employeeId.Value);

        var employees = await employeesQuery.OrderBy(x => x.MaNV)
            .Select(x => new ScheduleEmployeeViewModel { Id = x.MaNV, Name = x.HoTen, Position = x.ChucVu, Phone = x.SoDienThoai })
            .ToListAsync();

        var periodAssignments = await context.LichCaLamViecs.AsNoTracking()
            .Include(x => x.NhanVien).Include(x => x.CaLamViec)
            .Where(x => x.NgayLam >= start && x.NgayLam < end.AddDays(1))
            .ToListAsync();

        var employeeIds = employees.Select(x => x.Id).ToHashSet();
        var visible = periodAssignments.Where(x => employeeIds.Contains(x.MaNV)
            && (string.IsNullOrWhiteSpace(status) || x.TrangThai == status)).ToList();
        var shifts = await context.CaLamViecs.AsNoTracking().Where(x => x.TrangThai).OrderBy(x => x.GioBatDau)
            .Select(x => new ScheduleShiftOptionViewModel { Id = x.MaCa, Name = x.TenCa, Start = x.GioBatDau, End = x.GioKetThuc, Color = x.MauSac })
            .ToListAsync();

        return View(new ScheduleCalendarViewModel
        {
            StartDate = start, EndDate = end, IsDayView = dayView, Search = search, StatusFilter = status,
            EmployeeFilter = employeeId,
            Dates = Enumerable.Range(0, dayView ? 1 : 7).Select(offset => start.AddDays(offset)).ToList(),
            Employees = employees,
            Shifts = shifts,
            Assignments = visible.Select(Map).OrderBy(x => x.Date).ThenBy(x => x.Start).ToList(),
            TotalAssignments = periodAssignments.Count,
            PendingCount = periodAssignments.Count(x => x.TrangThai == "ChoXacNhan"),
            ConfirmedCount = periodAssignments.Count(x => x.TrangThai == "DaXacNhan"),
            RejectedCount = periodAssignments.Count(x => x.TrangThai == "TuChoi")
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSchedule(CreateScheduleViewModel model)
    {
        var start = model.StartDate.Date;
        var end = model.EndDate.Date;
        if (end < start || (end - start).TotalDays > 30)
            return ScheduleError("Khoảng ngày xếp lịch phải từ 1 đến 31 ngày.", start);
        if (!AllowedStatuses.Contains(model.Status)) return ScheduleError("Trạng thái ca không hợp lệ.", start);
        if (model.DaysOfWeek.Count == 0) return ScheduleError("Chọn ít nhất một ngày trong tuần.", start);
        if (model.EmployeeIds is null || model.EmployeeIds.Count == 0)
            return ScheduleError("Chọn ít nhất một nhân viên.", start);

        var shift = await context.CaLamViecs.FirstOrDefaultAsync(x => x.MaCa == model.ShiftId && x.TrangThai);
        if (shift is null) return ScheduleError("Ca làm đã bị ngừng hoạt động hoặc không tồn tại.", start);
        var employees = await context.NhanViens.Where(x => model.EmployeeIds.Contains(x.MaNV) && x.TrangThai).ToListAsync();
        if (employees.Count != model.EmployeeIds.Distinct().Count()) return ScheduleError("Có nhân viên không tồn tại hoặc đã nghỉ việc.", start);

        var days = Enumerable.Range(0, (end - start).Days + 1).Select(offset => start.AddDays(offset))
            .Where(day => model.DaysOfWeek.Contains((int)day.DayOfWeek)).ToList();
        if (days.Count == 0) return ScheduleError("Không có ngày nào khớp với các thứ đã chọn.", start);

        var conflicts = new List<string>();
        var activeAssignments = await context.LichCaLamViecs.AsNoTracking()
            .Include(x => x.CaLamViec)
            .Where(x => employees.Select(e => e.MaNV).Contains(x.MaNV)
                && x.NgayLam >= start.AddDays(-1) && x.NgayLam < end.AddDays(2)
                && x.TrangThai != "TuChoi")
            .ToListAsync();
        var candidates = new List<LichCaLamViec>();

        foreach (var employee in employees)
        foreach (var day in days)
        {
            var candidateRange = ShiftRange(day, shift.GioBatDau, shift.GioKetThuc);
            var overlapsExisting = activeAssignments.Where(x => x.MaNV == employee.MaNV)
                .Any(x => Overlaps(candidateRange, ShiftRange(x.NgayLam, x.CaLamViec!.GioBatDau, x.CaLamViec.GioKetThuc)));
            var overlapsNew = candidates.Where(x => x.MaNV == employee.MaNV)
                .Any(x => Overlaps(candidateRange, ShiftRange(x.NgayLam, shift.GioBatDau, shift.GioKetThuc)));
            if (overlapsExisting || overlapsNew)
            {
                conflicts.Add($"{employee.HoTen} · {day:dd/MM}");
                continue;
            }
            candidates.Add(new LichCaLamViec
            {
                MaNV = employee.MaNV, MaCa = shift.MaCa, NgayLam = day,
                TrangThai = model.Status, GhiChu = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim()
            });
        }

        if (conflicts.Count > 0)
            return ScheduleError("Không xếp được do trùng giờ: " + string.Join(", ", conflicts.Take(8)) + (conflicts.Count > 8 ? "…" : ""), start);

        await using var transaction = await context.Database.BeginTransactionAsync();
        context.LichCaLamViecs.AddRange(candidates);
        try
        {
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Đã xếp {candidates.Count} ca làm.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Lịch vừa thay đổi hoặc ca đã được xếp trùng. Hãy tải lại lịch và thử lại.";
        }
        return RedirectToAction(nameof(Index), new { weekStart = start, dayView = false });
    }

    [HttpGet]
    public async Task<IActionResult> EditSchedule(int id)
    {
        var item = await context.LichCaLamViecs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        return View(new EditScheduleViewModel
        {
            Id = item.Id, EmployeeId = item.MaNV, ShiftId = item.MaCa, Date = item.NgayLam,
            Status = item.TrangThai, Note = item.GhiChu,
            Employees = await GetEmployeesAsync(), Shifts = await GetShiftsAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSchedule(EditScheduleViewModel model)
    {
        var item = await context.LichCaLamViecs.Include(x => x.CaLamViec).FirstOrDefaultAsync(x => x.Id == model.Id);
        if (item is null) return NotFound();
        var employee = await context.NhanViens.FirstOrDefaultAsync(x => x.MaNV == model.EmployeeId && x.TrangThai);
        var shift = await context.CaLamViecs.FirstOrDefaultAsync(x => x.MaCa == model.ShiftId && x.TrangThai);
        if (employee is null || shift is null || !AllowedStatuses.Contains(model.Status))
            ModelState.AddModelError(string.Empty, "Nhân viên, ca làm hoặc trạng thái không hợp lệ.");
        if (ModelState.IsValid && model.Status != "TuChoi" && await HasOverlapAsync(model.EmployeeId, model.Date.Date, shift!, item.Id))
            ModelState.AddModelError(string.Empty, "Nhân viên đã có ca khác trùng giờ trong ngày này.");
        if (!ModelState.IsValid)
        {
            model.Employees = await GetEmployeesAsync(); model.Shifts = await GetShiftsAsync();
            return View(model);
        }
        item.MaNV = model.EmployeeId; item.MaCa = model.ShiftId; item.NgayLam = model.Date.Date;
        item.TrangThai = model.Status; item.GhiChu = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật ca làm.";
        return RedirectToAction(nameof(Index), new { weekStart = item.NgayLam });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, DateTime weekStart)
    {
        var item = await context.LichCaLamViecs.Include(x => x.CaLamViec).FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        if (!AllowedStatuses.Contains(status)) return BadRequest();
        if (status != "TuChoi" && await HasOverlapAsync(item.MaNV, item.NgayLam, item.CaLamViec!, item.Id))
        {
            TempData["ErrorMessage"] = "Không thể xác nhận: nhân viên có ca khác trùng giờ.";
            return RedirectToAction(nameof(Index), new { weekStart });
        }
        item.TrangThai = status;
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật trạng thái ca.";
        return RedirectToAction(nameof(Index), new { weekStart });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSchedule(int id, DateTime weekStart)
    {
        var item = await context.LichCaLamViecs.FindAsync(id);
        if (item is null) return NotFound();
        context.LichCaLamViecs.Remove(item);
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã xóa ca khỏi lịch.";
        return RedirectToAction(nameof(Index), new { weekStart });
    }

    public async Task<IActionResult> Ca()
    {
        ViewBag.CaLams = await context.CaLamViecs.AsNoTracking().OrderByDescending(x => x.TrangThai).ThenBy(x => x.GioBatDau).ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShift(ShiftTemplateViewModel model)
    {
        if (!ValidShiftTime(model.Start, model.End)) ModelState.AddModelError(string.Empty, "Giờ kết thúc phải khác giờ bắt đầu.");
        if (!ModelState.IsValid) return ShiftFormError();
        if (await context.CaLamViecs.AnyAsync(x => x.TenCa == model.Name.Trim()))
        {
            TempData["ErrorMessage"] = "Tên ca đã tồn tại.";
            return RedirectToAction(nameof(Ca));
        }
        context.CaLamViecs.Add(new CaLamViec
        {
            TenCa = model.Name.Trim(), GioBatDau = model.Start, GioKetThuc = model.End,
            MauSac = model.Color, GhiChu = model.Note?.Trim(), TrangThai = true
        });
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã tạo loại ca làm.";
        return RedirectToAction(nameof(Ca));
    }

    [HttpGet]
    public async Task<IActionResult> EditShift(int id)
    {
        var shift = await context.CaLamViecs.FindAsync(id);
        if (shift is null) return NotFound();
        return View(new ShiftTemplateViewModel
        {
            Id = shift.MaCa, Name = shift.TenCa, Start = shift.GioBatDau, End = shift.GioKetThuc,
            Color = shift.MauSac, Note = shift.GhiChu, Active = shift.TrangThai
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditShift(ShiftTemplateViewModel model)
    {
        var shift = await context.CaLamViecs.FindAsync(model.Id);
        if (shift is null) return NotFound();
        if (!ValidShiftTime(model.Start, model.End)) ModelState.AddModelError(string.Empty, "Giờ kết thúc phải khác giờ bắt đầu.");
        if (!ModelState.IsValid) return View(model);
        if (await context.CaLamViecs.AnyAsync(x => x.MaCa != model.Id && x.TenCa == model.Name.Trim()))
            ModelState.AddModelError(nameof(model.Name), "Tên ca đã tồn tại.");
        if (!ModelState.IsValid) return View(model);
        shift.TenCa = model.Name.Trim(); shift.GioBatDau = model.Start; shift.GioKetThuc = model.End;
        shift.MauSac = model.Color; shift.GhiChu = model.Note?.Trim(); shift.TrangThai = model.Active;
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật ca làm.";
        return RedirectToAction(nameof(Ca));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleShift(int id)
    {
        var shift = await context.CaLamViecs.FindAsync(id);
        if (shift is null) return NotFound();
        shift.TrangThai = !shift.TrangThai;
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = shift.TrangThai ? "Đã mở lại ca làm." : "Đã ngừng sử dụng ca làm.";
        return RedirectToAction(nameof(Ca));
    }

    private IActionResult ScheduleError(string message, DateTime date)
    {
        TempData["ErrorMessage"] = message;
        return RedirectToAction(nameof(Index), new { weekStart = date });
    }

    private IActionResult ShiftFormError()
    {
        TempData["ErrorMessage"] = "Thông tin ca chưa hợp lệ. Kiểm tra tên, màu và giờ bắt đầu/kết thúc.";
        return RedirectToAction(nameof(Ca));
    }

    private Task<List<ScheduleEmployeeViewModel>> GetEmployeesAsync() => context.NhanViens.AsNoTracking()
        .Where(x => x.TrangThai).OrderBy(x => x.MaNV)
        .Select(x => new ScheduleEmployeeViewModel { Id = x.MaNV, Name = x.HoTen, Position = x.ChucVu }).ToListAsync();

    private Task<List<ScheduleShiftOptionViewModel>> GetShiftsAsync() => context.CaLamViecs.AsNoTracking()
        .Where(x => x.TrangThai).OrderBy(x => x.GioBatDau)
        .Select(x => new ScheduleShiftOptionViewModel { Id = x.MaCa, Name = x.TenCa, Start = x.GioBatDau, End = x.GioKetThuc, Color = x.MauSac }).ToListAsync();

    private async Task<bool> HasOverlapAsync(int employeeId, DateTime date, CaLamViec shift, int exceptId)
    {
        var range = ShiftRange(date.Date, shift.GioBatDau, shift.GioKetThuc);
        var existing = await context.LichCaLamViecs.AsNoTracking().Include(x => x.CaLamViec)
            .Where(x => x.MaNV == employeeId && x.Id != exceptId && x.TrangThai != "TuChoi"
                && x.NgayLam >= date.Date.AddDays(-1) && x.NgayLam < date.Date.AddDays(2))
            .ToListAsync();
        return existing.Any(x => Overlaps(range, ShiftRange(x.NgayLam, x.CaLamViec!.GioBatDau, x.CaLamViec.GioKetThuc)));
    }

    private static bool ValidShiftTime(TimeSpan start, TimeSpan end) => start >= TimeSpan.Zero && start < TimeSpan.FromDays(1)
        && end >= TimeSpan.Zero && end < TimeSpan.FromDays(1) && start != end;

    private static (DateTime Start, DateTime End) ShiftRange(DateTime date, TimeSpan start, TimeSpan end)
    {
        var begins = date.Date.Add(start);
        var finishes = date.Date.Add(end <= start ? end.Add(TimeSpan.FromDays(1)) : end);
        return (begins, finishes);
    }

    private static bool Overlaps((DateTime Start, DateTime End) a, (DateTime Start, DateTime End) b) => a.Start < b.End && b.Start < a.End;

    private static ScheduleAssignmentViewModel Map(LichCaLamViec x) => new()
    {
        Id = x.Id, EmployeeId = x.MaNV, EmployeeName = x.NhanVien?.HoTen ?? "Nhân viên",
        Position = x.NhanVien?.ChucVu ?? string.Empty, ShiftId = x.MaCa, ShiftName = x.CaLamViec?.TenCa ?? "Ca đã ngừng",
        Date = x.NgayLam, Start = x.CaLamViec?.GioBatDau ?? TimeSpan.Zero, End = x.CaLamViec?.GioKetThuc ?? TimeSpan.Zero,
        Color = x.CaLamViec?.MauSac ?? "#198754", Status = x.TrangThai, Note = x.GhiChu
    };
}
