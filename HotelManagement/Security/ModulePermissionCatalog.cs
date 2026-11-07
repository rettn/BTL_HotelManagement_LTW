namespace HotelManagement.Security;

public sealed record ModulePermission(string Key, string Name, string Description);

public static class ModulePermissionCatalog
{
    public const string ClaimType = "hotel:module";

    public static readonly IReadOnlyList<ModulePermission> All = new[]
    {
        new ModulePermission("dashboard", "Tổng quan", "Trang tổng quan và thống kê."),
        new ModulePermission("bookings", "Đặt phòng", "Danh sách đặt phòng, nhận phòng và trả phòng."),
        new ModulePermission("rooms", "Sơ đồ và loại phòng", "Theo dõi phòng và quản lý loại phòng."),
        new ModulePermission("customers", "Khách hàng", "Hồ sơ và thông tin khách hàng."),
        new ModulePermission("inventory", "Kho và dịch vụ", "Hàng hóa, dịch vụ và nhập kho."),
        new ModulePermission("staff", "Nhân sự", "Hồ sơ nhân viên và chức vụ."),
        new ModulePermission("payroll", "Lương và điều chỉnh", "Bảng lương, thưởng, phụ cấp và khấu trừ."),
        new ModulePermission("finance", "Hóa đơn và thanh toán", "Hóa đơn và giao dịch thanh toán."),
        new ModulePermission("reports", "Báo cáo", "Báo cáo tổng quan và tài chính.")
    };

    public static string? GetModule(string? controller, string? action) => controller?.ToLowerInvariant() switch
    {
        "dashboard" when action?.Equals("TaiChinh", StringComparison.OrdinalIgnoreCase) == true => "reports",
        "dashboard" => "dashboard",
        "datphong" => "bookings",
        "phong" or "loaiphong" => "rooms",
        "khachhang" => "customers",
        "kho" or "dichvu" => "inventory",
        "nhanvien" or "chucvu" => "staff",
        "bangluong" or "thuong" or "phucap" or "khautru" => "payroll",
        "hoadon" => "finance",
        "report" => "reports",
        _ => null
    };
}
