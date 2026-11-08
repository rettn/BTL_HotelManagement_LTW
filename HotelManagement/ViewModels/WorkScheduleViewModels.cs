using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels;

public sealed class ScheduleCalendarViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsDayView { get; set; }
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
    public int? EmployeeFilter { get; set; }
    public List<DateTime> Dates { get; set; } = new();
    public List<ScheduleEmployeeViewModel> Employees { get; set; } = new();
    public List<ScheduleAssignmentViewModel> Assignments { get; set; } = new();
    public List<ScheduleShiftOptionViewModel> Shifts { get; set; } = new();
    public int TotalAssignments { get; set; }
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int RejectedCount { get; set; }
}

public sealed class ScheduleEmployeeViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public sealed class ScheduleShiftOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Color { get; set; } = "#198754";
}

public sealed class ScheduleAssignmentViewModel
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Color { get; set; } = "#198754";
    public string Status { get; set; } = "ChoXacNhan";
    public string? Note { get; set; }
}

public sealed class CreateScheduleViewModel
{
    [Required]
    public int ShiftId { get; set; }

    [Required, MinLength(1, ErrorMessage = "Chọn ít nhất một nhân viên.")]
    public List<int> EmployeeIds { get; set; } = new();

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Required, StringLength(20)]
    public string Status { get; set; } = "ChoXacNhan";

    [StringLength(250)]
    public string? Note { get; set; }

    public List<int> DaysOfWeek { get; set; } = new() { 0, 1, 2, 3, 4, 5, 6 };
}

public sealed class EditScheduleViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Range(1, int.MaxValue)]
    public int ShiftId { get; set; }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "ChoXacNhan";

    [StringLength(250)]
    public string? Note { get; set; }

    public List<ScheduleEmployeeViewModel> Employees { get; set; } = new();
    public List<ScheduleShiftOptionViewModel> Shifts { get; set; } = new();
}

public sealed class ShiftTemplateViewModel
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }

    [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Màu phải có dạng #RRGGBB.")]
    public string Color { get; set; } = "#198754";

    [StringLength(250)]
    public string? Note { get; set; }

    public bool Active { get; set; } = true;
}
