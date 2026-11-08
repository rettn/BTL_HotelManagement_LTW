using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    public class DatPhongKhachHangController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DatPhongService _datPhongService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SePayService _sePayService;

        public DatPhongKhachHangController(
            ApplicationDbContext context,
            DatPhongService datPhongService,
            UserManager<IdentityUser> userManager,
            SePayService sePayService)
        {
            _context = context;
            _datPhongService = datPhongService;
            _userManager = userManager;
            _sePayService = sePayService;
        }

        // GET: /DatPhongKhachHang
        // Khách hàng không cần đăng nhập vẫn có thể truy cập và điền form đặt phòng
        [HttpGet]
        public async Task<IActionResult> Index(int? maPhong, int? maLoaiPhong)
        {
            var model = new DatPhongKhachHangViewModel
            {
                NgayNhan = DateTime.Today,
                NgayTra = DateTime.Today.AddDays(1),
                PhuongThucThanhToan = "VietQR"
            };

            // Nếu người dùng đã đăng nhập thì tự động điền sẵn thông tin
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var email = user.Email ?? user.UserName;
                    var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == email);
                    if (khachHang != null)
                    {
                        model.HoTen = khachHang.HoTen;
                        model.Email = khachHang.Email ?? email ?? string.Empty;
                        model.SoDienThoai = khachHang.SoDienThoai ?? string.Empty;
                        model.CCCD = khachHang.CCCD;
                        model.GioiTinh = khachHang.GioiTinh;
                        model.NgaySinh = khachHang.NgaySinh;
                    }
                    else
                    {
                        model.Email = email ?? string.Empty;
                    }
                }
            }

            ViewBag.CaiDat = await _context.CaiDatHeThongs.FirstOrDefaultAsync();
            await NapDuLieuPhongAsync(model, maPhong, maLoaiPhong);
            return View(model);
        }

        // POST: /DatPhongKhachHang
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DatPhongKhachHangViewModel model)
        {
            // Kiểm tra ràng buộc ngày
            if (model.NgayTra.Date <= model.NgayNhan.Date)
            {
                ModelState.AddModelError(nameof(model.NgayTra), "Ngày trả phòng phải sau ngày nhận phòng ít nhất 1 ngày.");
            }

            var caiDat = await _context.CaiDatHeThongs.FirstOrDefaultAsync();
            var maxNgayDatTruoc = caiDat?.MaxDatTruoc ?? 90;

            if (model.NgayNhan.Date > DateTime.Today.AddDays(maxNgayDatTruoc))
            {
                ModelState.AddModelError(nameof(model.NgayNhan), $"Khách sạn chỉ nhận đặt phòng trước tối đa {maxNgayDatTruoc} ngày.");
            }

            if (model.NgayNhan.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.NgayNhan), "Ngày nhận phòng không thể ở trong quá khứ.");
            }

            // (Lưu ý: Nếu bạn đã đổi form sang chọn nhiều phòng, chỗ này cần kiểm tra model.DanhSachMaPhong thay vì model.MaPhong)
            if (model.MaPhong <= 0)
            {
                ModelState.AddModelError(nameof(model.MaPhong), "Vui lòng chọn một phòng còn trống để đặt.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CaiDat = caiDat;
                await NapDuLieuPhongAsync(model, model.MaPhong, null);
                return View(model);
            }

            KhachHang? khachHang = null;

            // 1. Nếu có tài khoản đăng nhập
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                var email = user?.Email ?? user?.UserName ?? model.Email;
                khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == email);
            }

            // 2. Nếu khách vãng lai hoặc chưa có bản ghi KhachHang, tìm theo SĐT hoặc Email
            if (khachHang == null && !string.IsNullOrWhiteSpace(model.SoDienThoai))
            {
                khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == model.SoDienThoai.Trim());
            }

            if (khachHang == null && !string.IsNullOrWhiteSpace(model.Email))
            {
                khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == model.Email.Trim());
            }

            // 3. Nếu chưa có thì tạo mới bản ghi khách hàng
            if (khachHang == null)
            {
                khachHang = new KhachHang
                {
                    HoTen = model.HoTen.Trim(),
                    Email = model.Email.Trim(),
                    SoDienThoai = model.SoDienThoai.Trim(),
                    CCCD = model.CCCD?.Trim(),
                    GioiTinh = model.GioiTinh,
                    NgaySinh = model.NgaySinh,
                    NgayTao = DateTime.Now
                };
                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Cập nhật thông tin khách hàng nếu có thay đổi
                var modified = false;
                if (!string.IsNullOrWhiteSpace(model.HoTen) && khachHang.HoTen != model.HoTen.Trim())
                {
                    khachHang.HoTen = model.HoTen.Trim();
                    modified = true;
                }
                if (!string.IsNullOrWhiteSpace(model.SoDienThoai) && khachHang.SoDienThoai != model.SoDienThoai.Trim())
                {
                    khachHang.SoDienThoai = model.SoDienThoai.Trim();
                    modified = true;
                }
                if (!string.IsNullOrWhiteSpace(model.CCCD) && khachHang.CCCD != model.CCCD.Trim())
                {
                    khachHang.CCCD = model.CCCD.Trim();
                    modified = true;
                }
                if (!string.IsNullOrWhiteSpace(model.Email) && khachHang.Email != model.Email.Trim())
                {
                    khachHang.Email = model.Email.Trim();
                    modified = true;
                }
                if (!string.IsNullOrWhiteSpace(model.GioiTinh) && khachHang.GioiTinh != model.GioiTinh)
                {
                    khachHang.GioiTinh = model.GioiTinh;
                    modified = true;
                }
                if (model.NgaySinh.HasValue && khachHang.NgaySinh != model.NgaySinh)
                {
                    khachHang.NgaySinh = model.NgaySinh;
                    modified = true;
                }

                if (modified)
                {
                    _context.KhachHangs.Update(khachHang);
                    await _context.SaveChangesAsync();
                }
            }

            // Ghi chú hình thức thanh toán
            var ptttText = "[Thanh toán tiền cọc qua VietQR]";

            var ghiChuDayDu = $"{ptttText} [Khách tự đặt trực tuyến] {model.GhiChu?.Trim()}".Trim();

            // Lấy cấu hình từ DB (đã lấy caiDat ở trên)
            caiDat ??= await _context.CaiDatHeThongs.FirstOrDefaultAsync();

            // Xử lý giờ nhận/trả phòng từ cấu hình (mặc định 14:00 và 12:00 nếu lỗi)
            TimeSpan gioNhan = TimeSpan.TryParse(caiDat?.GioNhanPhong, out var gn) ? gn : new TimeSpan(14, 0, 0);
            TimeSpan gioTra = TimeSpan.TryParse(caiDat?.GioTraPhong, out var gt) ? gt : new TimeSpan(12, 0, 0);

            // Gắn giờ vào ngày khách chọn
            DateTime ngayNhanDuKien = model.NgayNhan.Date.Add(gioNhan);
            DateTime ngayTraDuKien = model.NgayTra.Date.Add(gioTra);

            // Tính tổng tiền phòng dự kiến để tính cọc
            decimal tongTienDuKien = 0;
            int soDem = (model.NgayTra.Date - model.NgayNhan.Date).Days;
            if (soDem <= 0) soDem = 1;

            var phongDcChon = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(p => p.MaPhong == model.MaPhong);

            if (phongDcChon != null && phongDcChon.LoaiPhong != null)
            {
                tongTienDuKien = phongDcChon.LoaiPhong.GiaTheoDem * soDem;
            }

            // Lấy % cọc từ cấu hình
            decimal phanTramCoc = caiDat?.TienCocMacDinh ?? 30; // Mặc định 30%
            // Giá phòng trực tuyến áp dụng VAT 10%, giống hóa đơn Manager.
            decimal tienPhongVaThue = tongTienDuKien * 1.10m;
            decimal tienCoc = tienPhongVaThue * (phanTramCoc / 100m);

            // Tạo đặt phòng qua DatPhongService
            var datPhongVM = new DatPhongViewModel
            {
                MaKH = khachHang.MaKH,
                NgayNhan = ngayNhanDuKien, // Sử dụng ngày đã gắn giờ từ cấu hình
                NgayTra = ngayTraDuKien,   // Sử dụng ngày đã gắn giờ từ cấu hình
                TienCoc = tienCoc,
                GhiChu = ghiChuDayDu,
                MaPhongs = new List<int> { model.MaPhong }
            };

            var result = await _datPhongService.CreateAsync(datPhongVM);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await NapDuLieuPhongAsync(model, model.MaPhong, null);
                return View(model);
            }

            // Lấy mã đặt phòng vừa tạo
            var phieuMoi = await _context.DatPhongs
                .Where(d => d.MaKH == khachHang.MaKH)
                .OrderByDescending(d => d.MaDatPhong)
                .FirstOrDefaultAsync();

            int maDatPhong = phieuMoi?.MaDatPhong ?? 0;

            TempData["Success"] = "Đặt phòng trực tuyến thành công! Vui lòng xem thông tin chi tiết.";
            return RedirectToAction(nameof(Success), new { id = maDatPhong });
        }
        // GET: /DatPhongKhachHang/Success/5
        // Cho phép khách vãng lai xem chi tiết phiếu đặt phòng và quét mã QR VietQR để thanh toán
        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(c => c.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == id);

            if (datPhong == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Tính toán số tiền
            var chiTiet = datPhong.ChiTietDatPhongs.FirstOrDefault();
            var soDem = (datPhong.NgayTraDuKien.Date - datPhong.NgayNhanDuKien.Date).Days;
            if (soDem <= 0) soDem = 1;
            var donGia = chiTiet != null ? chiTiet.DonGia : (chiTiet?.Phong?.LoaiPhong?.GiaTheoDem ?? 0);
            var tongTien = donGia * soDem;

            var settings = _sePayService.GetSettings();
            var caiDatGiuPhong = await _context.CaiDatHeThongs.FirstOrDefaultAsync();
            var thoiHanGiuPhong = Math.Clamp(caiDatGiuPhong?.ThoiHanGiuPhong ?? 15, 5, 120);
            var hanThanhToan = datPhong.NgayDat.AddMinutes(thoiHanGiuPhong);
            var tiLe = Math.Clamp(caiDatGiuPhong?.TienCocMacDinh ?? 30, 0, 100);
            var soTienThanhToan = tongTien * 1.10m * tiLe / 100m;
            var tienThue = tongTien * 0.10m;
            var tongTienCoThue = tongTien + tienThue;

            // Tạo mã QR VietQR
            var vietQrUrl = _sePayService.GenerateVietQrUrl(datPhong.MaDatPhong, soTienThanhToan);

            ViewBag.Settings = settings;
            ViewBag.SoDem = soDem;
            ViewBag.TongTien = tongTien;
            ViewBag.TienThue = tienThue;
            ViewBag.TongTienCoThue = tongTienCoThue;
            ViewBag.SoTienThanhToan = soTienThanhToan;
            ViewBag.VietQrUrl = vietQrUrl;
            ViewBag.NoiDungChuyenKhoan = $"{settings.NoiDungPrefix}{datPhong.MaDatPhong}";
            ViewBag.HanThanhToan = hanThanhToan;
            ViewBag.ThoiHanGiuPhong = thoiHanGiuPhong;

            return View(datPhong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GiaiPhongPhieuChuaThanhToan(int id)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                .FirstOrDefaultAsync(d => d.MaDatPhong == id);

            if (datPhong == null)
                return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });

            var caiDat = await _context.CaiDatHeThongs.FirstOrDefaultAsync();
            var thoiHan = Math.Clamp(caiDat?.ThoiHanGiuPhong ?? 15, 5, 120);
            var hanThanhToan = datPhong.NgayDat.AddMinutes(thoiHan);

            if (datPhong.TrangThai == "ChoXacNhan" && DateTime.Now >= hanThanhToan)
            {
                var result = await _datPhongService.CancelAsync(id);
                return Json(new { success = result.Success, message = result.Message });
            }

            return Json(new { success = false, message = "Phiếu chưa đến thời hạn giải phóng hoặc đã được thanh toán." });
        }

        // GET: /DatPhongKhachHang/KiemTraThanhToan?maDatPhong=5
        // API tự động được frontend gọi mỗi 3 giây để kiểm tra giao dịch qua SePay
        [HttpGet]
        public async Task<IActionResult> KiemTraThanhToan(int maDatPhong)
        {
            if (maDatPhong <= 0)
            {
                return Json(new { isPaid = false, message = "Mã đặt phòng không hợp lệ." });
            }

            // Kiểm tra hết hạn ở phía server mỗi lần polling. Như vậy phiếu
            // vẫn được hủy và phòng được trả lại kể cả khi khách đóng trang
            // trước lúc đồng hồ đếm ngược kết thúc.
            var datPhong = await _context.DatPhongs
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null)
            {
                return Json(new { isPaid = false, expired = true, message = "Không tìm thấy phiếu đặt phòng." });
            }

            var caiDat = await _context.CaiDatHeThongs
                .AsNoTracking()
                .FirstOrDefaultAsync();
            var thoiHanGiuPhong = Math.Clamp(caiDat?.ThoiHanGiuPhong ?? 15, 5, 120);
            var hanThanhToan = datPhong.NgayDat.AddMinutes(thoiHanGiuPhong);

            if (datPhong.TrangThai == "ChoXacNhan" && DateTime.Now >= hanThanhToan)
            {
                var resultHuy = await _datPhongService.CancelAsync(maDatPhong);
                return Json(new
                {
                    isPaid = false,
                    expired = true,
                    cancelled = resultHuy.Success,
                    message = resultHuy.Message
                });
            }

            var result = await _sePayService.CheckPaymentAsync(maDatPhong);
            return Json(result);
        }

        // POST: /DatPhongKhachHang/GiaLapThanhToan?maDatPhong=5
        // Nút hỗ trợ chấm bài/thử nghiệm: Giả lập đã nhận chuyển khoản thành công
        [HttpPost]
        public async Task<IActionResult> GiaLapThanhToan(int maDatPhong)
        {
            if (maDatPhong <= 0)
            {
                return Json(new { success = false, message = "Mã đặt phòng không hợp lệ." });
            }

            var success = await _sePayService.SimulateSuccessPaymentAsync(maDatPhong);
            return Json(new { success, message = success ? "Giả lập thanh toán SePay thành công!" : "Không tìm thấy phòng." });
        }

        // GET: /DatPhongKhachHang/LichSu
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> LichSu()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var email = user.Email ?? user.UserName;
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == email);

            if (khachHang == null)
            {
                return View(new List<DatPhong>());
            }

            var danhSach = await _context.DatPhongs
                .Where(d => d.MaKH == khachHang.MaKH)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(c => c.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(danhSach);
        }

        private async Task NapDuLieuPhongAsync(DatPhongKhachHangViewModel model, int? maPhongUuTien, int? maLoaiPhong)
        {
            model.DanhSachLoaiPhong = await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(l => l.GiaTheoDem)
                .ToListAsync();

            var query = _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai == "Trong" || p.TrangThai == "Trống");

            if (maLoaiPhong.HasValue && maLoaiPhong > 0)
            {
                query = query.Where(p => p.MaLoaiPhong == maLoaiPhong.Value);
            }

            var phongs = await query
                .OrderBy(p => p.LoaiPhong != null ? p.LoaiPhong.GiaTheoDem : 0)
                .ThenBy(p => p.SoPhong)
                .ToListAsync();

            model.DanhSachPhongTrong = phongs.Select(p => new PhongKhachHangItem
            {
                MaPhong = p.MaPhong,
                SoPhong = p.SoPhong,
                MaLoaiPhong = p.MaLoaiPhong,
                TenLoaiPhong = p.LoaiPhong?.TenLoaiPhong ?? "Tiêu chuẩn",
                GiaTheoDem = p.LoaiPhong?.GiaTheoDem ?? 0,
                SoNguoiToiDa = p.LoaiPhong?.SoNguoiToiDa ?? 2,
                MoTa = p.LoaiPhong?.MoTa ?? p.GhiChu
            }).ToList();

            if (maPhongUuTien.HasValue && model.DanhSachPhongTrong.Any(p => p.MaPhong == maPhongUuTien.Value))
            {
                model.MaPhong = maPhongUuTien.Value;
            }
            else if (model.MaPhong <= 0 && model.DanhSachPhongTrong.Count > 0)
            {
                model.MaPhong = model.DanhSachPhongTrong[0].MaPhong;
            }
        }
    }
}
