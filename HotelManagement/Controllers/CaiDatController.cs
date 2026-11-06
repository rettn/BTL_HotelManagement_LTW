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

        public CaiDatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hàm hỗ trợ: Luôn lấy ra dòng cài đặt duy nhất (Id = 1), nếu chưa có thì tự tạo
        private CaiDatHeThong GetCurrentSettings()
        {
            var settings = _context.CaiDatHeThongs.FirstOrDefault();
            if (settings == null)
            {
                settings = new CaiDatHeThong();
                _context.CaiDatHeThongs.Add(settings);
                _context.SaveChanges();
            }
            return settings;
        }

        public IActionResult Index() => View();

        // ================= 1. THÔNG TIN KHÁCH SẠN =================
        [HttpGet]
        public IActionResult ThongTinKhachSan() => View(GetCurrentSettings());

        [HttpPost]
        public IActionResult ThongTinKhachSan(CaiDatHeThong model)
        {
            var settings = GetCurrentSettings();
            settings.TenKhachSan = model.TenKhachSan;
            settings.DiaChi = model.DiaChi;
            settings.SoDienThoai = model.SoDienThoai;
            settings.Email = model.Email;
            settings.ThongTinHoaDon = model.ThongTinHoaDon;

            _context.SaveChanges();
            TempData["Success"] = "Đã lưu Thông tin khách sạn!";
            return RedirectToAction(nameof(ThongTinKhachSan));
        }

        // ================= 2. QUY ĐỊNH LƯU TRÚ =================
        [HttpGet]
        public IActionResult QuyDinhLuuTru() => View(GetCurrentSettings());

        [HttpPost]
        public IActionResult QuyDinhLuuTru(CaiDatHeThong model)
        {
            var settings = GetCurrentSettings();
            settings.GioNhanPhong = model.GioNhanPhong;
            settings.GioTraPhong = model.GioTraPhong;
            settings.ThoiGianTraMuon = model.ThoiGianTraMuon;
            settings.TienCocMacDinh = model.TienCocMacDinh;
            settings.ChinhSachHuy = model.ChinhSachHuy;

            _context.SaveChanges();
            TempData["Success"] = "Đã lưu Quy định lưu trú!";
            return RedirectToAction(nameof(QuyDinhLuuTru));
        }

        // ================= 3. CẤU HÌNH ĐẶT PHÒNG =================
        [HttpGet]
        public IActionResult CauHinhDatPhong() => View(GetCurrentSettings());

        [HttpPost]
        public IActionResult CauHinhDatPhong(CaiDatHeThong model)
        {
            var settings = GetCurrentSettings();
            settings.MaxDatTruoc = model.MaxDatTruoc;
            settings.SoKhachMacDinh = model.SoKhachMacDinh;
            settings.ThoiHanGiuPhong = model.ThoiHanGiuPhong;

            _context.SaveChanges();
            TempData["Success"] = "Đã lưu Cấu hình đặt phòng!";
            return RedirectToAction(nameof(CauHinhDatPhong));
        }

        // ================= 4. THÔNG BÁO =================
        [HttpGet]
        public IActionResult ThongBao() => View(GetCurrentSettings());

        [HttpPost]
        public IActionResult ThongBao(CaiDatHeThong model, List<string> KenhThongBao)
        {
            var settings = GetCurrentSettings();
            settings.ThongBaoDatMoi = model.ThongBaoDatMoi;
            settings.ThongBaoSapNhanTra = model.ThongBaoSapNhanTra;
            settings.ThongBaoKho = model.ThongBaoKho;
            settings.KenhThongBao = string.Join(",", KenhThongBao); // Nối "System,Email" bằng dấu phẩy

            _context.SaveChanges();
            TempData["Success"] = "Đã lưu Cấu hình thông báo!";
            return RedirectToAction(nameof(ThongBao));
        }
    }
}