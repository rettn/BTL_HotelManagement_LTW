using Microsoft.AspNetCore.Authorization;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class DatPhongController : Controller
    {
        private readonly DatPhongService _service;
        private readonly ApplicationDbContext _context;
        private readonly PaymentSettingService _paymentSettingService;

        public DatPhongController(
            DatPhongService service,
            ApplicationDbContext context,
            PaymentSettingService paymentSettingService)
        {
            _service = service;
            _context = context;
            _paymentSettingService = paymentSettingService;
        }

        // GET: /DatPhong
        public async Task<IActionResult> Index()
        {
            var datPhongs = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                    .ThenInclude(p => p!.LoaiPhong)
                .Include(dp => dp.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.DanhSachLuuTru)
                .Include(dp => dp.HoaDons)
                    .ThenInclude(h => h.ThanhToans)
                .Where(dp => dp.TrangThai != "DaHuy")
                .OrderByDescending(dp => dp.MaDatPhong)
                .ToListAsync();

            return View(datPhongs);
        }

        // GET: /DatPhong/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var datPhong = await _service.GetByIdAsync(id);

            if (datPhong == null)
                return NotFound();

            return View(datPhong);
        }

        // GET: /DatPhong/Create
        public async Task<IActionResult> Create(int? maPhong)
        {
            var model = new DatPhongViewModel
            {
                NgayNhan = DateTime.Today,
                NgayTra = DateTime.Today.AddDays(1),
                MaPhongs = maPhong.HasValue
                    ? new List<int> { maPhong.Value }
                    : new List<int>()
            };

            await NapDuLieuFormAsync(model);
            return View(model);
        }

        // POST: /DatPhong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DatPhongViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await NapDuLieuFormAsync(model);
                return View(model);
            }

            var result = await _service.CreateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await NapDuLieuFormAsync(model);
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: /DatPhong/GetThongTinHuyPhong?id=5
        [HttpGet]
        public async Task<IActionResult> GetThongTinHuyPhong(int id)
        {
            var datPhong = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(dp => dp.MaDatPhong == id);

            if (datPhong == null)
                return NotFound(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });

            if (datPhong.TrangThai == "DaHuy")
                return BadRequest(new { success = false, message = "Phiếu đặt phòng này đã được hủy trước đó." });

            if (datPhong.TrangThai == "DaThanhToan" || datPhong.TrangThai == "DaHoanThanh")
                return BadRequest(new { success = false, message = "Không thể hủy phòng đã thanh toán trả phòng." });

            var ngayNhan = datPhong.NgayNhanDuKien;
            var ngayHienTai = DateTime.Now;
            var soNgayTruocNhan = (ngayNhan.Date - ngayHienTai.Date).TotalDays;

            double tiLeHoan = 0;
            string quyDinh = "";

            if (datPhong.TienCoc <= 0)
            {
                tiLeHoan = 0;
                quyDinh = "Khách hàng chưa đóng tiền cọc, không phát sinh khoản hoàn trả.";
            }
            else if (soNgayTruocNhan >= 7)
            {
                tiLeHoan = 100;
                quyDinh = $"Hủy trước {soNgayTruocNhan:0} ngày (>= 7 ngày): Đủ điều kiện hoàn 100% tiền cọc.";
            }
            else if (soNgayTruocNhan >= 3)
            {
                tiLeHoan = 50;
                quyDinh = $"Hủy trước {soNgayTruocNhan:0} ngày (3 - 6 ngày): Phí hủy phòng 50%, hoàn lại 50% tiền cọc.";
            }
            else
            {
                tiLeHoan = 0;
                quyDinh = $"Hủy trước {(soNgayTruocNhan < 0 ? 0 : soNgayTruocNhan):0} ngày (< 3 ngày): Theo chính sách không được hoàn lại tiền cọc.";
            }

            decimal soTienHoanGoiY = (datPhong.TienCoc * (decimal)tiLeHoan) / 100m;

            return Json(new
            {
                success = true,
                maDatPhong = datPhong.MaDatPhong,
                tenKhachHang = datPhong.KhachHang?.HoTen ?? "Khách lẻ",
                soDienThoai = datPhong.KhachHang?.SoDienThoai ?? "",
                ngayNhanDuKien = datPhong.NgayNhanDuKien.ToString("dd/MM/yyyy"),
                soNgayTruocNhan = (int)soNgayTruocNhan,
                tienCoc = datPhong.TienCoc,
                tiLeHoan = tiLeHoan,
                soTienHoanGoiY = soTienHoanGoiY,
                quyDinh = quyDinh,
                nhanVienThucHien = User.Identity?.Name ?? "Nhân viên trực"
            });
        }

        // POST: /DatPhong/XacNhanHuyPhong
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanHuyPhong([FromBody] HuyPhongRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.LyDoHuy))
                return BadRequest(new { success = false, message = "Vui lòng nhập lý do hủy phòng." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var datPhong = await _context.DatPhongs
                    .Include(dp => dp.ChiTietDatPhongs)
                    .FirstOrDefaultAsync(dp => dp.MaDatPhong == request.MaDatPhong);

                if (datPhong == null)
                    return NotFound(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });

                if (datPhong.TrangThai == "DaHuy")
                    return BadRequest(new { success = false, message = "Phiếu đặt phòng đã được hủy trước đó." });

                datPhong.TrangThai = "DaHuy";
                datPhong.GhiChu = $"[HỦY PHÒNG {DateTime.Now:dd/MM/yyyy HH:mm}] Lý do: {request.LyDoHuy}. Hoàn: {request.SoTienHoan:N0} đ";

                if (datPhong.TienCoc > 0)
                {
                    var hoaDon = await _context.HoaDons
                        .Include(h => h.ThanhToans)
                        .FirstOrDefaultAsync(h => h.MaDatPhong == datPhong.MaDatPhong);

                    if (hoaDon == null)
                    {
                        hoaDon = new HoaDon
                        {
                            MaDatPhong = datPhong.MaDatPhong,
                            NgayLap = DateTime.Now,
                            TienPhong = 0,
                            TienDichVu = 0,
                            GiamGia = 0,
                            TongTien = datPhong.TienCoc,
                            TrangThai = "DaHuy"
                        };
                        _context.HoaDons.Add(hoaDon);
                        await _context.SaveChangesAsync();
                    }

                    var thanhToan = hoaDon.ThanhToans.FirstOrDefault();
                    if (thanhToan == null)
                    {
                        thanhToan = new ThanhToan
                        {
                            MaHD = hoaDon.MaHD,
                            SoTien = datPhong.TienCoc,
                            PhuongThuc = "TienCoc",
                            TrangThai = "DaCoc",
                            NgayThanhToan = DateTime.Now,
                            MaGiaoDich = "COC" + datPhong.MaDatPhong
                        };
                        _context.ThanhToans.Add(thanhToan);
                        await _context.SaveChangesAsync();
                    }

                    var hoanTien = new HoanTien
                    {
                        MaThanhToan = thanhToan.MaThanhToan,
                        SoTienHoan = request.SoTienHoan,
                        LyDo = $"[Hình thức: {request.HinhThucHoan}] [Tỷ lệ hoàn: {request.PhanTramHoan}%] [Nhân viên: {User.Identity?.Name ?? "Nhân viên"}] Lý do: {request.LyDoHuy}",
                        TrangThai = request.SoTienHoan > 0 ? "DaHoanTien" : "KhongHoanTien",
                        NgayYeuCau = DateTime.Now,
                        NgayHoanTien = DateTime.Now
                    };
                    _context.HoanTiens.Add(hoanTien);
                }

                var maPhongList = datPhong.ChiTietDatPhongs.Select(c => c.MaPhong).Distinct().ToList();
                foreach (var maPhong in maPhongList)
                {
                    var phong = await _context.Phongs.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                    if (phong != null)
                    {
                        var dangCoLuuTru = await _context.LuuTrus
                            .AnyAsync(l => l.ChiTietDatPhong != null &&
                                           l.ChiTietDatPhong.MaPhong == maPhong &&
                                           l.ThoiGianTraPhong == null);

                        var conDatPhongKhac = await _context.ChiTietDatPhongs
                            .AnyAsync(c => c.MaPhong == maPhong &&
                                           c.MaDatPhong != datPhong.MaDatPhong &&
                                           c.DatPhong != null &&
                                           c.DatPhong.TrangThai != "DaHuy" &&
                                           c.DatPhong.TrangThai != "DaThanhToan" &&
                                           c.DatPhong.TrangThai != "DaHoanThanh");

                        if (!dangCoLuuTru && !conDatPhongKhac)
                        {
                            phong.TrangThai = "Trong";
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Hủy phòng và ghi nhận thông tin hoàn cọc thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Lỗi khi xử lý: " + ex.Message });
            }
        }

        // POST: /DatPhong/Cancel/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _service.CancelAsync(id);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Json(new { success = true, message = result.Message });
        }

        // GET: /DatPhong/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var datPhong = await _service.GetByIdAsync(id);
            if (datPhong == null)
                return NotFound();

            if (datPhong.TrangThai != "DaHuy")
            {
                TempData["Error"] = "Chỉ có thể xóa phiếu đã hủy.";
                return RedirectToAction(nameof(Index));
            }

            return View(datPhong);
        }

        // POST: /DatPhong/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int maDatPhong)
        {
            var result = await _service.DeleteAsync(maDatPhong);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // POST: /DatPhong/CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int id, int maPhong)
        {
            var result = await _service.CheckInAsync(id, maPhong);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Json(await BuildTrangThaiResponseAsync(id, maPhong, result.Message));
        }

        // POST: /DatPhong/CheckInAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInAll(int id)
        {
            var maPhongs = await _context.ChiTietDatPhongs.AsNoTracking()
                .Where(c => c.MaDatPhong == id &&
                            !c.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null))
                .OrderBy(c => c.MaPhong)
                .Select(c => c.MaPhong)
                .ToListAsync();

            if (maPhongs.Count == 0)
                return BadRequest(new { success = false, message = "Không còn phòng nào cần nhận." });

            var daNhan = 0;
            foreach (var maPhong in maPhongs)
            {
                var result = await _service.CheckInAsync(id, maPhong);
                if (!result.Success)
                {
                    var message = daNhan == 0
                        ? result.Message
                        : $"Đã nhận {daNhan}/{maPhongs.Count} phòng, dừng lại vì lỗi: {result.Message}";
                    return BadRequest(new { success = false, message });
                }
                daNhan++;
            }

            return Json(new { success = true, message = $"Đã nhận {daNhan} phòng." });
        }

        // POST: /DatPhong/CheckOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int id, int maPhong)
        {
            var result = await _service.CheckOutAsync(id, maPhong);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Json(await BuildTrangThaiResponseAsync(id, maPhong, result.Message));
        }

        [HttpGet]
        public async Task<IActionResult> GetThongTinTraPhong(int id, int maPhong)
        {
            var booking = await _context.DatPhongs.AsNoTracking()
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(c => c.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(c => c.DanhSachLuuTru)
                .FirstOrDefaultAsync(d => d.MaDatPhong == id);

            if (booking == null)
                return NotFound(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });

            var rooms = booking.ChiTietDatPhongs.Select(c =>
            {
                var stay = c.DanhSachLuuTru
                    .Where(l => l.ThoiGianNhanPhong != null && l.ThoiGianTraPhong == null)
                    .OrderByDescending(l => l.ThoiGianNhanPhong)
                    .FirstOrDefault();
                return new
                {
                    maPhong = c.MaPhong,
                    soPhong = c.Phong?.SoPhong ?? "",
                    tenLoaiPhong = c.Phong?.LoaiPhong?.TenLoaiPhong ?? "",
                    ngayNhan = stay?.ThoiGianNhanPhong?.ToString("dd/MM/yyyy HH:mm") ?? c.NgayNhan.ToString("dd/MM/yyyy"),
                    ngayTraDuKien = (c.NgayTra.TimeOfDay == TimeSpan.Zero ? c.NgayTra.Date.AddHours(12) : c.NgayTra).ToString("dd/MM/yyyy HH:mm"),
                    dangLuuTru = stay != null,
                    daTra = c.DanhSachLuuTru.Any(l => l.ThoiGianTraPhong != null)
                };
            }).OrderBy(r => r.soPhong).ToList();

            if (!rooms.Any(r => r.maPhong == maPhong && r.dangLuuTru))
                return BadRequest(new { success = false, message = "Phòng này hiện không có khách đang lưu trú." });

            return Json(new
            {
                success = true,
                maDatPhong = booking.MaDatPhong,
                tenKhachHang = booking.KhachHang?.HoTen ?? "Khách lẻ",
                rooms
            });
        }

        // POST: /DatPhong/CheckOutAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOutAll(int id)
        {
            var maPhongs = await _context.ChiTietDatPhongs.AsNoTracking()
                .Where(c => c.MaDatPhong == id &&
                            c.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null && l.ThoiGianTraPhong == null))
                .OrderBy(c => c.MaPhong)
                .Select(c => c.MaPhong)
                .ToListAsync();

            if (maPhongs.Count == 0)
                return BadRequest(new { success = false, message = "Không có phòng nào đang lưu trú để trả." });

            var daTra = 0;
            foreach (var maPhong in maPhongs)
            {
                var result = await _service.CheckOutAsync(id, maPhong);
                if (!result.Success)
                {
                    var message = daTra == 0
                        ? result.Message
                        : $"Đã trả {daTra}/{maPhongs.Count} phòng, dừng lại vì lỗi: {result.Message}";
                    return BadRequest(new { success = false, message });
                }
                daTra++;
            }

            return Json(new { success = true, message = $"Đã trả {daTra} phòng." });
        }

        private async Task<object> BuildTrangThaiResponseAsync(int maDatPhong, int maPhong, string message)
        {
            var phong = await _context.Phongs.AsNoTracking()
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);
            var datPhong = await _context.DatPhongs.AsNoTracking()
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            var trangThaiPhong = phong?.TrangThai?.Trim();
            var dangO = await _context.LuuTrus.AsNoTracking().AnyAsync(l =>
                l.ChiTietDatPhong != null &&
                l.ChiTietDatPhong.MaDatPhong == maDatPhong &&
                l.ChiTietDatPhong.MaPhong == maPhong &&
                l.ThoiGianNhanPhong != null &&
                l.ThoiGianTraPhong == null);

            string nutTiepTheo = dangO
                ? "TraPhong"
                : (trangThaiPhong == "DangDon" || trangThaiPhong == "Đang dọn dẹp")
                    ? "DonDep"
                    : "NhanPhong";

            return new
            {
                success = true,
                message,
                maDatPhong,
                maPhong,
                trangThaiPhong,
                trangThaiDatPhong = datPhong?.TrangThai,
                nutTiepTheo
            };
        }

        // POST: /DatPhong/DonDepXong
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DonDepXong(int maPhong)
        {
            var phong = await _context.Phongs
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

            if (phong == null)
                return BadRequest(new { success = false, message = "Không tìm thấy phòng." });

            var trangThai = phong.TrangThai?.Trim();
            if (trangThai != "DangDon" && trangThai != "Đang dọn dẹp")
                return BadRequest(new { success = false, message = "Phòng không ở trạng thái đang dọn dẹp." });

            phong.TrangThai = "Trong";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Phòng đã dọn dẹp xong và sẵn sàng đón khách." });
        }

        // GET: /DatPhong/DichVuLuuTru?maPhong=3
        [HttpGet]
        public async Task<IActionResult> DichVuLuuTru(int maPhong)
        {
            var luuTru = await _context.LuuTrus
                .AsNoTracking()
                .Include(l => l.ChiTietDatPhong)
                    .ThenInclude(c => c!.Phong)
                .Include(l => l.ChiTietDichVus)
                    .ThenInclude(d => d.DichVu)
                .Where(l =>
                    l.ChiTietDatPhong != null &&
                    l.ChiTietDatPhong.MaPhong == maPhong &&
                    l.ThoiGianNhanPhong != null &&
                    l.ThoiGianTraPhong == null)
                .OrderByDescending(l => l.ThoiGianNhanPhong)
                .FirstOrDefaultAsync();

            if (luuTru == null)
            {
                return Content(
                    "<div class='type-empty'><p>Phòng này hiện không có khách đang lưu trú.</p></div>",
                    "text/html");
            }

            ViewBag.DanhSachDichVu = await _context.DichVus
                .AsNoTracking()
                .Where(d => d.TrangThai)
                .OrderBy(d => d.TenDichVu)
                .ToListAsync();

            return PartialView("_DichVuLuuTru", luuTru);
        }

        // POST: /DatPhong/ThemDichVu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemDichVu(int maLuuTru, int maDichVu, int soLuong)
        {
            if (soLuong < 1 || soLuong > 100)
                return BadRequest(new { success = false, message = "Số lượng phải từ 1 đến 100." });

            var luuTru = await _context.LuuTrus
                .FirstOrDefaultAsync(l => l.MaLuuTru == maLuuTru);

            if (luuTru == null || luuTru.ThoiGianTraPhong != null)
                return BadRequest(new { success = false, message = "Phòng không còn khách đang lưu trú." });

            var dichVu = await _context.DichVus
                .FirstOrDefaultAsync(d => d.Id == maDichVu && d.TrangThai);

            if (dichVu == null)
                return BadRequest(new { success = false, message = "Dịch vụ không tồn tại hoặc đã ngừng cung cấp." });

            _context.ChiTietDichVus.Add(new ChiTietDichVu
            {
                MaLuuTru = luuTru.MaLuuTru,
                MaDichVu = dichVu.Id,
                SoLuong = soLuong,
                DonGia = dichVu.Gia,
                ThoiGianSuDung = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã thêm dịch vụ." });
        }
        // POST: /DatPhong/SuaDichVu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaDichVu(int maChiTietDichVu, int maDichVu, int soLuong)
        {
            if (soLuong < 1 || soLuong > 100)
                return BadRequest(new { success = false, message = "Số lượng phải từ 1 đến 100." });

            var chiTiet = await _context.ChiTietDichVus
                .FirstOrDefaultAsync(c => c.MaChiTietDichVu == maChiTietDichVu);

            if (chiTiet == null)
                return NotFound(new { success = false, message = "Không tìm thấy dịch vụ cần sửa." });

            var luuTru = await _context.LuuTrus
                .FirstOrDefaultAsync(l => l.MaLuuTru == chiTiet.MaLuuTru);

            if (luuTru == null || luuTru.ThoiGianTraPhong != null)
                return BadRequest(new { success = false, message = "Khách đã trả phòng, không thể sửa dịch vụ." });

            // Đổi sang dịch vụ khác thì lấy đơn giá mới, giữ nguyên dịch vụ thì giữ đơn giá đã lưu
            if (maDichVu != chiTiet.MaDichVu)
            {
                var dichVu = await _context.DichVus
                    .FirstOrDefaultAsync(d => d.Id == maDichVu && d.TrangThai);

                if (dichVu == null)
                    return BadRequest(new { success = false, message = "Dịch vụ không tồn tại hoặc đã ngừng cung cấp." });

                chiTiet.MaDichVu = dichVu.Id;
                chiTiet.DonGia = dichVu.Gia;
            }

            chiTiet.SoLuong = soLuong;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã cập nhật dịch vụ." });
        }

        // POST: /DatPhong/XoaDichVu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaDichVu(int maChiTietDichVu)
        {
            var chiTiet = await _context.ChiTietDichVus
                .FirstOrDefaultAsync(c => c.MaChiTietDichVu == maChiTietDichVu);

            if (chiTiet == null)
                return NotFound(new { success = false, message = "Không tìm thấy dịch vụ cần xóa." });

            var luuTru = await _context.LuuTrus
                .FirstOrDefaultAsync(l => l.MaLuuTru == chiTiet.MaLuuTru);

            if (luuTru == null || luuTru.ThoiGianTraPhong != null)
                return BadRequest(new { success = false, message = "Khách đã trả phòng, không thể xóa dịch vụ." });

            _context.ChiTietDichVus.Remove(chiTiet);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa dịch vụ." });
        }

        // GET: /DatPhong/Checkout/5
        [HttpGet]
        public async Task<IActionResult> Checkout(int id)
        {
            var model = await GetHoaDonCheckoutViewModelAsync(id);
            if (model == null) return NotFound();

            return View(model);
        }

        // POST: /DatPhong/XacNhanCheckout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanCheckout(HoaDonCheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var currentModel = await GetHoaDonCheckoutViewModelAsync(model.MaDatPhong);
                return View("Checkout", currentModel ?? model);
            }

            var datPhong = await _context.DatPhongs
                .Include(dp => dp.ChiTietDatPhongs)
                .FirstOrDefaultAsync(dp => dp.MaDatPhong == model.MaDatPhong);

            if (datPhong != null)
            {
                // Chặn thanh toán / trả phòng lặp lại
                if (datPhong.TrangThai != "DangLuuTru" && datPhong.TrangThai != "DaHoanThanh")
                {
                    TempData["Error"] = "Chỉ có thể trả phòng và thanh toán cho phiếu đang lưu trú.";
                    return RedirectToAction(nameof(Index));
                }

                var thoiGianTra = DateTime.Now;
                datPhong.TrangThai = "DaThanhToan";
                datPhong.NgayTraThucTe = thoiGianTra;

                var dsChiTiet = datPhong.ChiTietDatPhongs.Select(c => c.MaChiTietDatPhong).ToList();

                // Đóng các lượt lưu trú đang mở để phòng không còn hiện là "có khách"
                var luuTruDangMo = await _context.LuuTrus
                    .Where(l => dsChiTiet.Contains(l.MaChiTietDatPhong) &&
                                l.ThoiGianNhanPhong != null &&
                                l.ThoiGianTraPhong == null)
                    .ToListAsync();

                foreach (var lt in luuTruDangMo)
                    lt.ThoiGianTraPhong = thoiGianTra;

                // Chỉ phòng đã được nhận mới chuyển sang đang dọn
                var chiTietDaNhan = luuTruDangMo.Select(l => l.MaChiTietDatPhong).ToHashSet();

                foreach (var ctdp in datPhong.ChiTietDatPhongs)
                {
                    var phong = await _context.Phongs.FindAsync(ctdp.MaPhong);
                    if (phong != null)
                    {
                        if (chiTietDaNhan.Contains(ctdp.MaChiTietDatPhong))
                            phong.TrangThai = "DangDon";
                        else if (phong.TrangThai != "DangDon" && phong.TrangThai != "Đang dọn dẹp")
                            phong.TrangThai = "Trong";
                    }
                }

                await _context.SaveChangesAsync();

                // Ghi nhận Hóa đơn và Thanh toán vào CSDL khi thanh toán thành công
                var existingHoaDon = await _context.HoaDons.FirstOrDefaultAsync(h => h.MaDatPhong == datPhong.MaDatPhong);
                if (existingHoaDon == null)
                {
                    var tinh = await GetHoaDonCheckoutViewModelAsync(datPhong.MaDatPhong);
                    var tongTien = tinh?.TongThanhToan ?? model.TongThanhToan;
                    var hoaDon = new HoaDon
                    {
                        MaDatPhong = datPhong.MaDatPhong,
                        NgayLap = thoiGianTra,
                        TienPhong = tinh?.TienPhong ?? 0,
                        TienDichVu = tinh?.TongTienDichVu ?? 0,
                        GiamGia = 0,
                        TongTien = tongTien,
                        TrangThai = "DaThanhToan"
                    };
                    _context.HoaDons.Add(hoaDon);
                    await _context.SaveChangesAsync();

                    var thanhToan = new ThanhToan
                    {
                        MaHD = hoaDon.MaHD,
                        SoTien = tongTien,
                        PhuongThuc = model.PhuongThucThanhToan ?? "TienMat",
                        TrangThai = "ThanhCong",
                        NgayThanhToan = thoiGianTra,
                        MaGiaoDich = "GD" + hoaDon.MaHD + DateTime.Now.ToString("yyyyMMddHHmmss")
                    };
                    _context.ThanhToans.Add(thanhToan);
                    await _context.SaveChangesAsync();
                }
            }

            // Lấy MaHD chuẩn xác từ database để truyền sang trang thành công
            var hoaDonVuaTao = await _context.HoaDons
                .FirstOrDefaultAsync(h => h.MaDatPhong == model.MaDatPhong);

            int maHDGoi = hoaDonVuaTao?.MaHD ?? model.MaDatPhong;

            return RedirectToAction(nameof(ThanhToanThanhCong), new { id = maHDGoi });
        }

       // Thanh toan hoa don thanh cong
        [HttpGet("DatPhong/ThanhToanThanhCong/{id?}")]
        public async Task<IActionResult> ThanhToanThanhCong(int id, int? maDatPhong)
        {
            int targetId = id > 0 ? id : (maDatPhong ?? 0);
            if (targetId == 0) return NotFound(); 

            // Tìm hóa đơn theo MaHD hoặc MaDatPhong
            var hoaDon = await _context.HoaDons
                .Include(h => h.DatPhong)
                .FirstOrDefaultAsync(h => h.MaHD == targetId || h.MaDatPhong == targetId);

            int realMaDatPhong = hoaDon?.MaDatPhong ?? targetId;

            var viewModel = await GetHoaDonCheckoutViewModelAsync(realMaDatPhong);
            if (viewModel == null)
            {
                return NotFound();
            }

            ViewBag.MaHoaDon = $"HD{maDatPhong}{DateTime.Now:yyyyMMdd}";
            return View("ThanhToanThanhCong", viewModel);
        }
        

        // HÀM BỔ TRỢ LẤY DỮ LIỆU BẢNG KÊ CHECKOUT & HÓA ĐƠN
        private async Task<HoaDonCheckoutViewModel?> GetHoaDonCheckoutViewModelAsync(int maDatPhong)
        {
            var datPhong = await _context.DatPhongs
                                .Include(dp => dp.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p.LoaiPhong)
                .Include(dp => dp.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.DanhSachLuuTru)
                .FirstOrDefaultAsync(dp => dp.MaDatPhong == maDatPhong);

            if (datPhong == null) return null;


            var cacLuuTru = datPhong.ChiTietDatPhongs
                                    .SelectMany(ct => ct.DanhSachLuuTru)
                                    .Where(l => l.ThoiGianNhanPhong != null)
                                    .ToList();

            DateTime ngayNhan = cacLuuTru.Count > 0
                ? cacLuuTru.Min(l => l.ThoiGianNhanPhong.GetValueOrDefault())
                : (datPhong.NgayNhanThucTe != default ? datPhong.NgayNhanThucTe : datPhong.NgayNhanDuKien);

            DateTime ngayTra = datPhong.NgayTraThucTe != default
                ? datPhong.NgayTraThucTe
                : (cacLuuTru.Count > 0 && cacLuuTru.All(l => l.ThoiGianTraPhong != null)
                    ? cacLuuTru.Max(l => l.ThoiGianTraPhong.GetValueOrDefault())
                    : DateTime.Now);
            int soDem = (int)(ngayTra.Date - ngayNhan.Date).TotalDays;
            if (soDem <= 0) soDem = 1;

            // Mỗi phòng trong phiếu là một dòng trên hóa đơn
            var danhSachPhong = datPhong.ChiTietDatPhongs
                .Where(ct => ct.Phong != null)
                .Where(ct => cacLuuTru.Count == 0 || ct.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null))
                .Select(ct =>
                {
                    var phongDat = ct.Phong!;
                    var giaMotDem = ct.DonGia > 0
                        ? ct.DonGia
                        : (phongDat.LoaiPhong?.GiaTheoDem ?? 0);

                    return new PhongHoaDonItemViewModel
                    {
                        SoPhong = phongDat.SoPhong,
                        TenLoaiPhong = phongDat.LoaiPhong?.TenLoaiPhong ?? "Tiêu chuẩn",
                        GiaTheoDem = giaMotDem,
                        ThanhTien = giaMotDem * soDem
                    };
                })
                .OrderBy(p => p.SoPhong)
                .ToList();

            decimal tienPhong = danhSachPhong.Sum(p => p.ThanhTien);

            string soPhongText = danhSachPhong.Count > 0
                ? string.Join(", ", danhSachPhong.Select(p => p.SoPhong))
                : "---";
            string loaiPhongText = danhSachPhong.Count > 0
                ? string.Join(", ", danhSachPhong.Select(p => p.TenLoaiPhong).Distinct())
                : "Tiêu chuẩn";

            // Tải chi tiết dịch vụ đã sử dụng
            var dsDichVu = await _context.ChiTietDichVus
                .AsNoTracking()
                .Include(cdv => cdv.DichVu)
                .Where(cdv => _context.LuuTrus.Any(l => l.MaLuuTru == cdv.MaLuuTru
                                                     && l.ChiTietDatPhong != null
                                                     && l.ChiTietDatPhong.MaDatPhong == maDatPhong))
                .Select(cdv => new ChiTietDichVuItemViewModel
                {
                    TenDichVu = cdv.DichVu != null ? cdv.DichVu.TenDichVu : "Dịch vụ",
                    SoLuong = cdv.SoLuong,
                    DonGia = cdv.DonGia,
                    ThanhTien = cdv.SoLuong * cdv.DonGia
                })
                .ToListAsync();

            decimal tongTienDichVu = dsDichVu.Sum(x => x.ThanhTien);
            decimal tiLeThuePercent = 10;
            decimal tienThue = (tienPhong + tongTienDichVu) * (tiLeThuePercent / 100m);
            decimal tongThanhToan = tienPhong + tongTienDichVu + tienThue;

            var paymentSetting = _paymentSettingService.GetSettings();
            string nganHangMa = paymentSetting?.NganHangMa ?? "";
            string soTaiKhoan = paymentSetting?.SoTaiKhoan ?? "";
            string tenChuTaiKhoan = paymentSetting?.TenChuTaiKhoan ?? "";
            string noiDungCK = $"{paymentSetting?.NoiDungPrefix}{datPhong.MaDatPhong}";

            string noiDungMaHoa = Uri.EscapeDataString(noiDungCK);
            string tenTaiKhoanMaHoa = Uri.EscapeDataString(tenChuTaiKhoan);
            string vietQrUrl = $"https://img.vietqr.io/image/{nganHangMa}-{soTaiKhoan}-compact2.png?amount={(long)tongThanhToan}&addInfo={noiDungMaHoa}&accountName={tenTaiKhoanMaHoa}";

            return new HoaDonCheckoutViewModel
            {
                MaDatPhong = datPhong.MaDatPhong,
                SoPhong = soPhongText,
                TenKhachHang = datPhong.KhachHang?.HoTen ?? "Khách vãng lai",
                SoDienThoai = datPhong.KhachHang?.SoDienThoai ?? "",
                TenLoaiPhong = loaiPhongText,
                NgayNhan = ngayNhan,
                NgayTraThucTe = ngayTra,
                SoDem = soDem,
                GiaTheoDem = danhSachPhong.Sum(p => p.GiaTheoDem),
                DanhSachPhong = danhSachPhong,
                TienPhong = tienPhong,
                DanhSachDichVu = dsDichVu,
                TongTienDichVu = tongTienDichVu,
                TiLeThuePercent = tiLeThuePercent,
                TienThue = tienThue,
                TongThanhToan = tongThanhToan,
                PhuongThucThanhToan = "TienMat",

                NganHangMa = nganHangMa,
                SoTaiKhoan = soTaiKhoan,
                TenChuTaiKhoan = tenChuTaiKhoan,
                NoiDungChuyenKhoan = noiDungCK,
                VietQrImageUrl = vietQrUrl
            };
        }

        private async Task NapDuLieuFormAsync(DatPhongViewModel model)
        {
            model.KhachHangs = await _context.KhachHangs
                .AsNoTracking()
                .OrderBy(k => k.HoTen)
                .Select(k => new KhachHangOption
                {
                    MaKH = k.MaKH,
                    HoTen = k.HoTen,
                    SoDienThoai = k.SoDienThoai
                })
                .ToListAsync();

            model.Phongs = await _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .OrderBy(p => p.SoPhong)
                .Select(p => new PhongOption
                {
                    MaPhong = p.MaPhong,
                    SoPhong = p.SoPhong,
                    TenLoaiPhong = p.LoaiPhong != null
                        ? p.LoaiPhong.TenLoaiPhong
                        : "Chưa phân loại",
                    GiaTheoDem = p.LoaiPhong != null
                        ? p.LoaiPhong.GiaTheoDem
                        : 0,
                    TrangThai = p.TrangThai
                })
                .ToListAsync();
        }

        // Cập nhật danh sách phòng khả dụng theo khoảng ngày lưu trú.
        [HttpGet]
        public async Task<IActionResult> PhongKhaDung(DateTime ngayNhan, DateTime ngayTra)
        {
            if (ngayTra.Date <= ngayNhan.Date || ngayNhan.Date < DateTime.Today)
                return BadRequest(new { message = "Khoảng ngày lưu trú không hợp lệ." });

            var rooms = await _context.Phongs.AsNoTracking()
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai != "DangSuDung" && p.TrangThai != "Đang sử dụng" &&
                            p.TrangThai != "DangDon" && p.TrangThai != "Đang dọn dẹp")
                .OrderBy(p => p.SoPhong)
                .Select(p => new
                {
                    p.MaPhong,
                    p.SoPhong,
                    TenLoaiPhong = p.LoaiPhong != null ? p.LoaiPhong.TenLoaiPhong : "Chưa phân loại",
                    GiaTheoDem = p.LoaiPhong != null ? p.LoaiPhong.GiaTheoDem : 0,
                    TrangThai = p.TrangThai
                })
                .ToListAsync();

            var maPhongDaDat = await _context.ChiTietDatPhongs.AsNoTracking()
                .Where(c => c.NgayNhan < ngayTra.Date && c.NgayTra > ngayNhan.Date &&
                            c.DatPhong != null && c.DatPhong.TrangThai != "DaHuy" &&
                            c.DatPhong.TrangThai != "DaThanhToan" && c.DatPhong.TrangThai != "DaHoanThanh")
                .Select(c => c.MaPhong)
                .Distinct()
                .ToListAsync();
            var blocked = maPhongDaDat.ToHashSet();

            return Json(rooms.Where(p => !blocked.Contains(p.MaPhong)));
        }

        [HttpGet]
        public async Task<IActionResult> XuatHoaDon(int id)
        {
            // Ở đây id có thể là MaHD hoặc MaDatPhong, ta tìm theo MaHD trước, nếu không thấy thì tìm theo MaDatPhong
            var hoaDon = await _context.HoaDons.FirstOrDefaultAsync(h => h.MaHD == id || h.MaDatPhong == id);
            int maDatPhong = hoaDon?.MaDatPhong ?? id;

            var viewModel = await GetHoaDonCheckoutViewModelAsync(maDatPhong);
            if (viewModel == null)
            {
                return NotFound("Không tìm thấy thông tin hóa đơn.");
            }

            ViewBag.MaHoaDon = hoaDon?.MaHD ?? id;
            return View("XuatHoaDon", viewModel);
        }

    }
}
