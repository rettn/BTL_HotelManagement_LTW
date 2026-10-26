using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class QuanTriController : Controller
    {
        private static readonly string[] VaiTroHopLe = { "Admin", "NhanVien" };
        private readonly UserManager<IdentityUser> _userManager;

        public QuanTriController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // ---------- Tra cứu + danh sách ----------
        public async Task<IActionResult> Index(string? keyword)
        {
            var q = _userManager.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                q = q.Where(u => u.Email!.Contains(keyword)
                              || (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)));
            }

            var users = await q.OrderBy(u => u.Email).ToListAsync();
            var list = new List<QuanTriItemViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                list.Add(new QuanTriItemViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    VaiTro = roles.FirstOrDefault() ?? "Chưa có",
                    BiKhoa = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow
                });
            }

            ViewBag.Keyword = keyword;
            ViewBag.CurrentUserId = _userManager.GetUserId(User);
            return View(list);
        }

        // ---------- Tạo tài khoản Admin / Nhân viên ----------
        [HttpGet]
        public IActionResult Create() => View(new QuanTriCreateViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuanTriCreateViewModel vm)
        {
            if (!VaiTroHopLe.Contains(vm.VaiTro))
                ModelState.AddModelError(nameof(vm.VaiTro), "Vai trò không hợp lệ.");
            if (!ModelState.IsValid) return View(vm);

            var user = new IdentityUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, vm.MatKhau);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", DichLoi(e));
                return View(vm);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, vm.VaiTro);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);   // hoàn tác
                foreach (var e in roleResult.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            TempData["Success"] = $"Đã tạo tài khoản {vm.Email} ({vm.VaiTro}).";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Sửa: số điện thoại + phân quyền ----------
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return View(new QuanTriEditViewModel
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                VaiTro = roles.Contains("Admin") ? "Admin" : "NhanVien"
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(QuanTriEditViewModel vm)
        {
            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            vm.Email = user.Email;
            if (!VaiTroHopLe.Contains(vm.VaiTro))
                ModelState.AddModelError(nameof(vm.VaiTro), "Vai trò không hợp lệ.");

            // Không cho Admin tự hạ quyền của chính mình (tránh hệ thống mất Admin)
            if (user.Id == _userManager.GetUserId(User) && vm.VaiTro != "Admin")
                ModelState.AddModelError(nameof(vm.VaiTro), "Bạn không thể tự hạ quyền của chính mình.");

            if (!ModelState.IsValid) return View(vm);

            await _userManager.SetPhoneNumberAsync(user, vm.PhoneNumber);

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(vm.VaiTro) || roles.Count != 1)
            {
                await _userManager.RemoveFromRolesAsync(user, roles);
                await _userManager.AddToRoleAsync(user, vm.VaiTro);
                await _userManager.UpdateSecurityStampAsync(user);   // buộc nạp lại quyền ở lần đăng nhập sau
            }

            TempData["Success"] = "Đã cập nhật tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Đặt lại mật khẩu ----------
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(new ResetPasswordViewModel { Id = user.Id, Email = user.Email });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
        {
            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            vm.Email = user.Email;
            if (!ModelState.IsValid) return View(vm);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, vm.MatKhauMoi);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", DichLoi(e));
                return View(vm);
            }

            TempData["Success"] = $"Đã đặt lại mật khẩu cho {user.Email}.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Khóa / mở khóa (thay cho xóa tài khoản) ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "Bạn không thể khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.SetLockoutEnabledAsync(user, true);

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Success"] = $"Đã mở khóa {user.Email}.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                await _userManager.UpdateSecurityStampAsync(user);   // đẩy phiên đang đăng nhập ra
                TempData["Success"] = $"Đã khóa {user.Email}.";
            }
            return RedirectToAction(nameof(Index));
        }

        private static string DichLoi(IdentityError e) => e.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Email này đã được sử dụng.",
            "PasswordTooShort" => "Mật khẩu quá ngắn.",
            "PasswordRequiresUpper" => "Mật khẩu cần ít nhất một chữ hoa.",
            "PasswordRequiresLower" => "Mật khẩu cần ít nhất một chữ thường.",
            "PasswordRequiresDigit" => "Mật khẩu cần ít nhất một chữ số.",
            "PasswordRequiresNonAlphanumeric" => "Mật khẩu cần ít nhất một ký tự đặc biệt.",
            _ => e.Description
        };
    }
}