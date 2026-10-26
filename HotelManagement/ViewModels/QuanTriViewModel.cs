using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels
{
    // ---------- Phần A: Admin quản trị ----------
    public class QuanTriItemViewModel
    {
        public string Id { get; set; } = "";
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string VaiTro { get; set; } = "";
        public bool BiKhoa { get; set; }
    }

    public class QuanTriCreateViewModel
    {
        [Required(ErrorMessage = "Nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string XacNhanMatKhau { get; set; } = "";

        [Required] public string VaiTro { get; set; } = "NhanVien";   // Admin | NhanVien
    }

    public class QuanTriEditViewModel
    {
        public string Id { get; set; } = "";
        public string? Email { get; set; }          // chỉ hiển thị

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [Required] public string VaiTro { get; set; } = "NhanVien";
    }

    public class ResetPasswordViewModel
    {
        public string Id { get; set; } = "";
        public string? Email { get; set; }          // chỉ hiển thị

        [Required(ErrorMessage = "Nhập mật khẩu mới")]
        [DataType(DataType.Password)]
        public string MatKhauMoi { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string XacNhanMatKhau { get; set; } = "";
    }

    // ---------- Phần B: Tài khoản của tôi ----------
    public class ProfileViewModel
    {
        public string? Email { get; set; }          // chỉ hiển thị
        public string? VaiTro { get; set; }         // chỉ hiển thị

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }
    }

    public class ChangeEmailViewModel
    {
        public string? EmailHienTai { get; set; }   // chỉ hiển thị

        [Required(ErrorMessage = "Nhập email mới")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string EmailMoi { get; set; } = "";

        [Required(ErrorMessage = "Nhập mật khẩu để xác nhận")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = "";
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        public string MatKhauHienTai { get; set; } = "";

        [Required(ErrorMessage = "Nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string MatKhauMoi { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string XacNhanMatKhauMoi { get; set; } = "";
    }
}