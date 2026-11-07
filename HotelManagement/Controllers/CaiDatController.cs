using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CaiDatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CaiDatController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Hàm hỗ trợ: Luôn lấy ra dòng cài đặt duy nhất (Id = 1), nếu chưa có thì tự tạo
        private CaiDatHeThong GetCurrentSettings()
        {
            var settings = _context.CaiDatHeThongs.FirstOrDefault();
            if (settings == null)
            {
                settings = new CaiDatHeThong
                {
                    TenKhachSan = "Khách Sạn Grand Lotus Luxury",
                    DiaChi = "123 Đường Trần Phú, Phường 1, TP. Nha Trang, Khánh Hòa",
                    SoDienThoai = "0379 868 686",
                    Email = "contact@grandlotushotel.com",
                    ThongTinHoaDon = "Công ty TNHH Khách Sạn Grand Lotus - MST: 0108998899",
                    GioNhanPhong = "14:00",
                    GioTraPhong = "12:00",
                    ThoiGianTraMuon = 6,
                    TienCocMacDinh = 30,
                    ChinhSachHuy = "Hủy phòng trước 24 giờ nhận phòng: Hoàn 100% tiền cọc.\nHủy trong vòng 24 giờ: Phí hủy tương đương 50% tiền cọc.\nVắng mặt (No-show): Không hoàn tiền cọc.",
                    MaxDatTruoc = 90,
                    SoKhachMacDinh = 2,
                    ThoiHanGiuPhong = 15,
                    ThongBaoDatMoi = true,
                    ThongBaoSapNhanTra = true,
                    ThongBaoKho = true,
                    KenhThongBao = "System"
                };
                _context.CaiDatHeThongs.Add(settings);
                _context.SaveChanges();
            }
            return settings;
        }

        public IActionResult Index()
        {
            var settings = GetCurrentSettings();
            return View(settings);
        }

        // ================= 1. THÔNG TIN KHÁCH SẠN =================
        [HttpGet]
        public IActionResult ThongTinKhachSan() => View(GetCurrentSettings());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThongTinKhachSan(CaiDatHeThong model, IFormFile? HinhAnhUpload, bool? XoaHinhAnh)
        {
            var settings = GetCurrentSettings();
            settings.TenKhachSan = model.TenKhachSan?.Trim();
            settings.DiaChi = model.DiaChi?.Trim();
            settings.SoDienThoai = model.SoDienThoai?.Trim();
            settings.Email = model.Email?.Trim();
            settings.ThongTinHoaDon = model.ThongTinHoaDon?.Trim();

            if (XoaHinhAnh == true)
            {
                // Xóa ảnh cũ nếu có file
                if (!string.IsNullOrEmpty(settings.HinhAnh))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, settings.HinhAnh.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        try { System.IO.File.Delete(oldPath); } catch { }
                    }
                }
                settings.HinhAnh = null;
            }
            else if (HinhAnhUpload != null && HinhAnhUpload.Length > 0)
            {
                var ext = Path.GetExtension(HinhAnhUpload.FileName).ToLowerInvariant();
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
                if (!allowedExts.Contains(ext))
                {
                    TempData["Error"] = "Định dạng ảnh không hợp lệ. Vui lòng chọn file .jpg, .jpeg, .png, .webp hoặc .svg.";
                    return View(settings);
                }

                if (HinhAnhUpload.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Kích thước ảnh vượt quá giới hạn 5 MB.";
                    return View(settings);
                }

                var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "hotel");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var fileName = $"hotel_logo_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N[..6]}{ext}";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await HinhAnhUpload.CopyToAsync(stream);
                }

                settings.HinhAnh = $"/uploads/hotel/{fileName}";
            }

            _context.SaveChanges();
            TempData["Success"] = "Đã cập nhật thông tin khách sạn và hình ảnh thành công!";
            return RedirectToAction(nameof(ThongTinKhachSan));
        }

        // ================= 2. QUY ĐỊNH LƯU TRÚ =================
        [HttpGet]
        public IActionResult QuyDinhLuuTru() => View(GetCurrentSettings());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuyDinhLuuTru(CaiDatHeThong model)
        {
            var settings = GetCurrentSettings();
            settings.GioNhanPhong = string.IsNullOrWhiteSpace(model.GioNhanPhong) ? "14:00" : model.GioNhanPhong;
            settings.GioTraPhong = string.IsNullOrWhiteSpace(model.GioTraPhong) ? "12:00" : model.GioTraPhong;
            settings.ThoiGianTraMuon = model.ThoiGianTraMuon <= 0 ? 6 : model.ThoiGianTraMuon;
            settings.TienCocMacDinh = model.TienCocMacDinh < 0 ? 0 : (model.TienCocMacDinh > 100 ? 100 : model.TienCocMacDinh);
            settings.ChinhSachHuy = model.ChinhSachHuy;

            _context.SaveChanges();
            TempData["Success"] = "Đã lưu Quy định lưu trú thành công!";
            return RedirectToAction(nameof(QuyDinhLuuTru));
        }

        // ================= 3. CẤU HÌNH ĐẶT PHÒNG =================
        [HttpGet]
        public IActionResult CauHinhDatPhong() => View(GetCurrentSettings());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CauHinhDatPhong(CaiDatHeThong model)
        {
            var settings = GetCurrentSettings();
            settings.MaxDatTruoc = model.MaxDatTruoc <= 0 ? 90 : model.MaxDatTruoc;
            settings.SoKhachMacDinh = model.SoKhachMacDinh <= 0 ? 2 : model.SoKhachMacDinh;
            settings.ThoiHanGiuPhong = model.ThoiHanGiuPhong <= 0 ? 15 : model.ThoiHanGiuPhong;

            _context.SaveChanges();
            TempData["Success"] = "Đã lưu Cấu hình đặt phòng thành công!";
            return RedirectToAction(nameof(CauHinhDatPhong));
        }

        // ================= 4. THÔNG BÁO =================
        [HttpGet]
        public IActionResult ThongBao() => View(GetCurrentSettings());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThongBao(CaiDatHeThong model, List<string>? KenhThongBao)
        {
            var settings = GetCurrentSettings();
            settings.ThongBaoDatMoi = model.ThongBaoDatMoi;
            settings.ThongBaoSapNhanTra = model.ThongBaoSapNhanTra;
            settings.ThongBaoKho = model.ThongBaoKho;
            settings.KenhThongBao = (KenhThongBao != null && KenhThongBao.Count > 0)
                ? string.Join(",", KenhThongBao)
                : "System";

            _context.SaveChanges();
            TempData["Success"] = "Đã lưu Cấu hình thông báo thành công!";
            return RedirectToAction(nameof(ThongBao));
        }
    }
}
