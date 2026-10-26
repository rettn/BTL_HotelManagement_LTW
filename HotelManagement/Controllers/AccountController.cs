using System.Security.Claims;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(SignInManager<IdentityUser> signInManager,
                                 UserManager<IdentityUser> userManager,
                                 ApplicationDbContext context,
                                 IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
        }

        // ============ ĐĂNG NHẬP / ĐĂNG XUẤT / TỪ CHỐI TRUY CẬP ============
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập email và mật khẩu.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                email, password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);

                // 1. Kiểm tra Role trước: Admin hoặc Nhân viên luôn bị ép về Dashboard
                if (user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "NhanVien")))
                {
                    return RedirectToAction("Index", "Dashboard");
                }

                // 2. Đối với khách hàng bình thường: Ưu tiên trả về trang họ đang muốn truy cập (returnUrl)
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // 3. Nếu khách hàng vào thẳng trang Login mà không có returnUrl, cho về trang mặc định
                return RedirectToAction("Index", "DatPhongKhachHang");
            }
            if (result.IsLockedOut)
            {
                ViewBag.Error = "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.";
                return View();
            }

            ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();

        // ============ TÀI KHOẢN CỦA TÔI ============
        [Authorize, HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            return View(await BuildProfileAsync(user));
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                vm.Email = user.Email;
                vm.VaiTro = string.Join(", ", await _userManager.GetRolesAsync(user));
                return View(vm);
            }

            var result = await _userManager.SetPhoneNumberAsync(user, vm.PhoneNumber);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                vm.Email = user.Email;
                vm.VaiTro = string.Join(", ", await _userManager.GetRolesAsync(user));
                return View(vm);
            }

            TempData["Success"] = "Đã cập nhật thông tin cá nhân.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> ChangeEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            return View(new ChangeEmailViewModel { EmailHienTai = user.Email });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            vm.EmailHienTai = user.Email;
            if (!ModelState.IsValid) return View(vm);

            if (!await _userManager.CheckPasswordAsync(user, vm.MatKhau))
            {
                ModelState.AddModelError(nameof(vm.MatKhau), "Mật khẩu không đúng.");
                return View(vm);
            }

            if (string.Equals(user.Email, vm.EmailMoi, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(vm.EmailMoi), "Email mới trùng với email hiện tại.");
                return View(vm);
            }

            var existing = await _userManager.FindByEmailAsync(vm.EmailMoi);
            if (existing != null && existing.Id != user.Id)
            {
                ModelState.AddModelError(nameof(vm.EmailMoi), "Email này đã được dùng bởi tài khoản khác.");
                return View(vm);
            }

            // Project dùng email làm tên đăng nhập nên đổi cả hai
            var r1 = await _userManager.SetEmailAsync(user, vm.EmailMoi);
            var r2 = r1.Succeeded ? await _userManager.SetUserNameAsync(user, vm.EmailMoi) : r1;
            if (!r2.Succeeded)
            {
                foreach (var e in r2.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Đã đổi email đăng nhập.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize, HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var result = await _userManager.ChangePasswordAsync(user, vm.MatKhauHienTai, vm.MatKhauMoi);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", DichLoi(e));
                return View(vm);
            }

            await _signInManager.RefreshSignInAsync(user);   // giữ đăng nhập
            TempData["Success"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Profile));
        }

        // ============ HÀM DÙNG CHUNG ============
        private async Task<ProfileViewModel> BuildProfileAsync(IdentityUser user) => new()
        {
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            VaiTro = string.Join(", ", await _userManager.GetRolesAsync(user))
        };

        private static string DichLoi(IdentityError e) => e.Code switch
        {
            "PasswordMismatch" => "Mật khẩu hiện tại không đúng.",
            "PasswordTooShort" => "Mật khẩu quá ngắn.",
            "PasswordRequiresUpper" => "Mật khẩu cần ít nhất một chữ hoa.",
            "PasswordRequiresLower" => "Mật khẩu cần ít nhất một chữ thường.",
            "PasswordRequiresDigit" => "Mật khẩu cần ít nhất một chữ số.",
            "PasswordRequiresNonAlphanumeric" => "Mật khẩu cần ít nhất một ký tự đặc biệt.",
            _ => e.Description
        };
    }
}