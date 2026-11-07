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
        private readonly SePayService _sePayService;

        public DatPhongController(
            DatPhongService service,
            ApplicationDbContext context,
            PaymentSettingService paymentSettingService,
            SePayService sePayService)
        {
            _service = service;
            _context = context;
            _paymentSettingService = paymentSettingService;
            _sePayService = sePayService;
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

            var caiDat = await _context.CaiDatHeThongs.FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(caiDat?.ChinhSachHuy))
            {
                quyDinh = $"{quyDinh}\nChính sách hủy của khách sạn: {caiDat.ChinhSachHuy}";
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
                                .Include(dp => dp.KhachHang)
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

            return Json(new
            {
                success = true,
                message = $"Đã trả {daTra} phòng.",
                redirectUrl = Url.Action(nameof(XuatHoaDon), new { id })
            });
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
                .Include(l => l.ChiTietDatPhong)
                    .ThenInclude(c => c!.DatPhong)
                        .ThenInclude(d => d!.KhachHang)
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

            var usedItemIds = luuTru.ChiTietDichVus.Select(x => x.MaDichVu).Distinct().ToList();
            ViewBag.DanhSachDichVu = await _context.DichVus
                .AsNoTracking()
                .Where(d => (d.TrangThai && (d.LoaiHang != "SanPham" || d.TonKho > 0)) || usedItemIds.Contains(d.Id))
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

            await using var transaction = await _context.Database.BeginTransactionAsync();
            if (dichVu.LoaiHang == "SanPham")
            {
                if (dichVu.TonKho < soLuong)
                    return BadRequest(new { success = false, message = $"Tồn kho {dichVu.TonKho} {dichVu.DonViTinh} không đủ." });
                dichVu.TonKho -= soLuong;
            }

            _context.ChiTietDichVus.Add(new ChiTietDichVu
            {
                MaLuuTru = luuTru.MaLuuTru,
                MaDichVu = dichVu.Id,
                SoLuong = soLuong,
                DonGia = dichVu.Gia,
                ThoiGianSuDung = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
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
            var oldDichVuId = chiTiet?.MaDichVu;
            var oldSoLuong = chiTiet?.SoLuong ?? 0;

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

            var oldItem = await _context.DichVus.FirstOrDefaultAsync(d => d.Id == oldDichVuId);
            var newItem = await _context.DichVus.FirstOrDefaultAsync(d => d.Id == chiTiet.MaDichVu);
            if (newItem == null)
                return BadRequest(new { success = false, message = "Hàng hóa không tồn tại." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            if (oldDichVuId == chiTiet.MaDichVu)
            {
                if (newItem.LoaiHang == "SanPham")
                {
                    var thayDoi = soLuong - oldSoLuong;
                    if (thayDoi > newItem.TonKho)
                        return BadRequest(new { success = false, message = $"Tồn kho {newItem.TonKho} {newItem.DonViTinh} không đủ." });
                    newItem.TonKho -= thayDoi;
                }
            }
            else
            {
                if (oldItem?.LoaiHang == "SanPham") oldItem.TonKho += oldSoLuong;
                if (newItem.LoaiHang == "SanPham")
                {
                    if (newItem.TonKho < soLuong)
                        return BadRequest(new { success = false, message = $"Tồn kho {newItem.TonKho} {newItem.DonViTinh} không đủ." });
                    newItem.TonKho -= soLuong;
                }
            }

            chiTiet.SoLuong = soLuong;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

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

            var item = await _context.DichVus.FirstOrDefaultAsync(d => d.Id == chiTiet.MaDichVu);
            if (item?.LoaiHang == "SanPham") item.TonKho += chiTiet.SoLuong;
            _context.ChiTietDichVus.Remove(chiTiet);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa dịch vụ." });
        }

        // GET: /DatPhong/Checkout/5
        [HttpGet]
        public async Task<IActionResult> Checkout(int id, int? maPhong, int[]? maPhongs)
        {
            var selectedRooms = maPhongs?.Distinct().ToList() ?? new List<int>();
            if (selectedRooms.Count == 0 && maPhong.HasValue) selectedRooms.Add(maPhong.Value);
            var model = selectedRooms.Count > 0
                ? await GetHoaDonCheckoutForRoomsAsync(id, selectedRooms)
                : await GetHoaDonCheckoutViewModelAsync(id);
            if (model == null) return NotFound();

            return View(model);
        }

        // GET: /DatPhong/KiemTraThanhToanCheckout?maDatPhong=5
        [HttpGet]
        public async Task<IActionResult> KiemTraThanhToanCheckout(int maDatPhong, int? maPhong, int[]? maPhongs)
        {
            var selectedRooms = maPhongs?.Distinct().ToList() ?? new List<int>();
            if (selectedRooms.Count == 0 && maPhong.HasValue) selectedRooms.Add(maPhong.Value);
            var checkoutInfo = selectedRooms.Count > 0
                ? await GetHoaDonCheckoutForRoomsAsync(maDatPhong, selectedRooms)
                : await GetHoaDonCheckoutViewModelAsync(maDatPhong);
            if (checkoutInfo == null)
            {
                return Json(new { isPaid = false, message = "Không tìm thấy thông tin checkout." });
            }

            var result = await _sePayService.CheckPaymentAsync(maDatPhong, checkoutInfo.SoTienConLai, ghiNhanTienCoc: false);
            return Json(result);
        }

        // POST: /DatPhong/XacNhanCheckout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanCheckout(HoaDonCheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var selectedRooms = model.MaPhongs?.Distinct().ToList() ?? new List<int>();
                if (selectedRooms.Count == 0 && model.MaPhong.HasValue) selectedRooms.Add(model.MaPhong.Value);
                var currentModel = selectedRooms.Count > 0
                    ? await GetHoaDonCheckoutForRoomsAsync(model.MaDatPhong, selectedRooms)
                    : await GetHoaDonCheckoutViewModelAsync(model.MaDatPhong);
                return View("Checkout", currentModel ?? model);
            }

            var datPhong = await _context.DatPhongs
                                .Include(dp => dp.KhachHang)
                .Include(dp => dp.ChiTietDatPhongs)
                    .ThenInclude(c => c.DanhSachLuuTru)
                .Include(dp => dp.ChiTietDatPhongs)
                    .ThenInclude(c => c.Phong)
                .FirstOrDefaultAsync(dp => dp.MaDatPhong == model.MaDatPhong);

            var hoaDonIdsVuaTao = new List<int>();
            if (datPhong != null)
            {
                // Chặn thanh toán / trả phòng lặp lại
                if (datPhong.TrangThai != "DangLuuTru"
                    && datPhong.TrangThai != "DaHoanThanh"
                    && datPhong.TrangThai != "DaCoc"
                    && datPhong.TrangThai != "DaThanhToan")
                {
                    TempData["Error"] = "Chỉ có thể trả phòng và thanh toán cho phiếu đang lưu trú.";
                    return RedirectToAction(nameof(Index));
                }

                var maPhongsThanhToan = model.MaPhongs?.Distinct().ToList() ?? new List<int>();
                if (maPhongsThanhToan.Count == 0 && model.MaPhong.HasValue) maPhongsThanhToan.Add(model.MaPhong.Value);
                if (maPhongsThanhToan.Count == 0)
                {
                    maPhongsThanhToan = datPhong.ChiTietDatPhongs
                        .Where(c => c.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null && l.ThoiGianTraPhong == null))
                        .Select(c => c.MaPhong).Distinct().ToList();
                }

                var chiTietThanhToan = datPhong.ChiTietDatPhongs
                    .Where(c => maPhongsThanhToan.Contains(c.MaPhong)
                        && c.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null && l.ThoiGianTraPhong == null))
                    .ToList();
                if (chiTietThanhToan.Count != maPhongsThanhToan.Count || chiTietThanhToan.Count == 0)
                {
                    TempData["Error"] = "Một hoặc nhiều phòng đã chọn không còn lượt lưu trú đang mở.";
                    return RedirectToAction(nameof(Index));
                }
                var roomPaymentModels = new List<HoaDonCheckoutViewModel>();
                foreach (var chiTiet in chiTietThanhToan)
                {
                    var roomModel = await GetHoaDonCheckoutViewModelAsync(datPhong.MaDatPhong, maPhong: chiTiet.MaPhong);
                    if (roomModel == null) return NotFound();
                    roomPaymentModels.Add(roomModel);
                }
                var checkoutInfoForPayment = await GetHoaDonCheckoutForRoomsAsync(datPhong.MaDatPhong, maPhongsThanhToan);
                if (checkoutInfoForPayment == null) return NotFound();

                if (string.Equals(model.PhuongThucThanhToan, "ChuyenKhoan", StringComparison.OrdinalIgnoreCase))
                {
                    var paymentResult = await _sePayService.CheckPaymentAsync(
                        datPhong.MaDatPhong,
                        checkoutInfoForPayment.SoTienConLai,
                        ghiNhanTienCoc: false);
                    if (!paymentResult.IsPaid)
                    {
                        TempData["Error"] = $"Chưa xác nhận được chuyển khoản: {paymentResult.Message}";
                        return RedirectToAction(nameof(Checkout), new { id = datPhong.MaDatPhong, maPhongs = maPhongsThanhToan.ToArray() });
                    }

                }

                var thoiGianTra = DateTime.Now;
                var luuTruDangMo = chiTietThanhToan.SelectMany(c => c.DanhSachLuuTru)
                    .Where(l => l.ThoiGianNhanPhong != null && l.ThoiGianTraPhong == null).ToList();
                foreach (var luuTru in luuTruDangMo) luuTru.ThoiGianTraPhong = thoiGianTra;

                foreach (var chiTiet in chiTietThanhToan)
                {
                    var phong = await _context.Phongs.FindAsync(chiTiet.MaPhong);
                    if (phong != null) phong.TrangThai = "DangDon";
                }

                var conLuuTru = await _context.LuuTrus.AnyAsync(l =>
                    l.ChiTietDatPhong != null && l.ChiTietDatPhong.MaDatPhong == datPhong.MaDatPhong
                    && l.ThoiGianNhanPhong != null && l.ThoiGianTraPhong == null);
                datPhong.NgayTraThucTe = thoiGianTra;
                if (!conLuuTru) datPhong.TrangThai = "DaThanhToan";
                else datPhong.TrangThai = "DangLuuTru";

                await _context.SaveChangesAsync();

                foreach (var roomModel in roomPaymentModels)
                {
                    var chiTietPhong = chiTietThanhToan.First(c => c.MaPhong == roomModel.MaPhong);
                    var hoaDon = new HoaDon
                    {
                        MaDatPhong = datPhong.MaDatPhong,
                        NgayLap = thoiGianTra,
                        TienPhong = roomModel.TienPhong,
                        TienDichVu = roomModel.TongTienDichVu,
                        GiamGia = 0,
                        TongTien = roomModel.TongThanhToan,
                        TrangThai = "DaThanhToan"
                    };
                    _context.HoaDons.Add(hoaDon);
                    await _context.SaveChangesAsync();
                    hoaDonIdsVuaTao.Add(hoaDon.MaHD);

                    foreach (var phongItem in roomModel.DanhSachPhong)
                    {
                        _context.ChiTietHoaDons.Add(new ChiTietHoaDon
                        {
                            MaHD = hoaDon.MaHD,
                            TenKhoanThu = $"Tiền phòng {phongItem.SoPhong}",
                            LoaiKhoanThu = "Phong",
                            MaThamChieu = chiTietPhong.MaChiTietDatPhong,
                            SoLuong = phongItem.SoDem,
                            DonGia = phongItem.GiaTheoDem,
                            ThanhTien = phongItem.ThanhTien
                        });
                    }
                    foreach (var dichVu in roomModel.DanhSachDichVu)
                    {
                        _context.ChiTietHoaDons.Add(new ChiTietHoaDon
                        {
                            MaHD = hoaDon.MaHD,
                            TenKhoanThu = dichVu.TenDichVu,
                            LoaiKhoanThu = "DichVu",
                            SoLuong = dichVu.SoLuong,
                            DonGia = dichVu.DonGia,
                            ThanhTien = dichVu.ThanhTien
                        });
                    }
                    _context.ThanhToans.Add(new ThanhToan
                    {
                        MaHD = hoaDon.MaHD,
                        SoTien = roomModel.SoTienConLai,
                        PhuongThuc = model.PhuongThucThanhToan ?? "TienMat",
                        TrangThai = "ThanhCong",
                        NgayThanhToan = thoiGianTra,
                        MaGiaoDich = "GD" + hoaDon.MaHD + thoiGianTra.ToString("yyyyMMddHHmmss")
                    });
                    await _context.SaveChangesAsync();
                    ViewBag.MaHoaDonThanhToanCuoi = hoaDon.MaHD;
                }
            }

            // Lấy MaHD chuẩn xác từ database để truyền sang trang thành công
            var hoaDonVuaTao = await _context.HoaDons
                .Where(h => h.MaDatPhong == model.MaDatPhong)
                .OrderByDescending(h => h.MaHD)
                .FirstOrDefaultAsync();

            if (hoaDonIdsVuaTao.Count > 1)
            {
                TempData["Success"] = $"Đã thanh toán và tạo {hoaDonIdsVuaTao.Count} hóa đơn riêng cho các phòng đã chọn.";
                return RedirectToAction("Index", "HoaDon", new { maDatPhong = model.MaDatPhong });
            }

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
                .Include(h => h.ThanhToans)
                .FirstOrDefaultAsync(h => h.MaHD == targetId)
                ?? await _context.HoaDons.Include(h => h.DatPhong).Include(h => h.ThanhToans)
                    .Where(h => h.MaDatPhong == targetId).OrderByDescending(h => h.MaHD).FirstOrDefaultAsync();

            int realMaDatPhong = hoaDon?.MaDatPhong ?? targetId;
            var maPhongHoaDon = hoaDon == null ? null : await _context.ChiTietHoaDons.AsNoTracking()
                .Where(c => c.MaHD == hoaDon.MaHD && c.LoaiKhoanThu == "Phong" && c.MaThamChieu.HasValue)
                .Select(c => (int?)_context.ChiTietDatPhongs
                    .Where(ct => ct.MaChiTietDatPhong == c.MaThamChieu.Value)
                    .Select(ct => ct.MaPhong)
                    .FirstOrDefault())
                .FirstOrDefaultAsync();

            var viewModel = await GetHoaDonCheckoutViewModelAsync(realMaDatPhong, cheDoXuatHoaDon: true, maPhong: maPhongHoaDon);
            if (viewModel == null)
            {
                return NotFound();
            }

            viewModel.PhuongThucThanhToan = hoaDon?.ThanhToans
                .OrderByDescending(t => t.NgayThanhToan)
                .Select(t => t.PhuongThuc)
                .FirstOrDefault() ?? "TienMat";

            ViewBag.MaHoaDon = hoaDon != null
                ? $"HD{hoaDon.MaHD}{hoaDon.NgayLap:yyyyMMdd}"
                : $"HD{realMaDatPhong}{DateTime.Now:yyyyMMdd}";
            return View("ThanhToanThanhCong", viewModel);
        }


        private async Task<HoaDonCheckoutViewModel?> GetHoaDonCheckoutForRoomsAsync(int maDatPhong, List<int> maPhongs)
        {
            var roomModels = new List<HoaDonCheckoutViewModel>();
            foreach (var maPhong in maPhongs.Distinct())
            {
                var roomModel = await GetHoaDonCheckoutViewModelAsync(maDatPhong, maPhong: maPhong);
                if (roomModel == null || roomModel.DanhSachPhong.Count == 0) return null;
                roomModels.Add(roomModel);
            }
            if (roomModels.Count == 0) return null;

            var combined = roomModels[0];
            combined.MaPhong = null;
            combined.MaPhongs = maPhongs.Distinct().ToList();
            combined.SoPhong = string.Join(", ", roomModels.SelectMany(m => m.DanhSachPhong).Select(p => p.SoPhong));
            combined.TenLoaiPhong = string.Join(", ", roomModels.SelectMany(m => m.DanhSachPhong).Select(p => p.TenLoaiPhong).Distinct());
            combined.DanhSachPhong = roomModels.SelectMany(m => m.DanhSachPhong).ToList();
            combined.DanhSachDichVu = roomModels.SelectMany(m => m.DanhSachDichVu).ToList();
            combined.TienPhong = roomModels.Sum(m => m.TienPhong);
            combined.TongTienDichVu = roomModels.Sum(m => m.TongTienDichVu);
            combined.TienThue = roomModels.Sum(m => m.TienThue);
            combined.TongThanhToan = roomModels.Sum(m => m.TongThanhToan);
            combined.TienCoc = roomModels.Sum(m => m.TienCoc);
            combined.DaThanhToanTruoc = roomModels.Sum(m => m.DaThanhToanTruoc);
            combined.SoTienConLai = roomModels.Sum(m => m.SoTienConLai);
            combined.NgayNhan = roomModels.Min(m => m.NgayNhan);
            combined.NgayTraThucTe = roomModels.Max(m => m.NgayTraThucTe);
            combined.SoDem = roomModels.Max(m => m.SoDem);
            combined.GiaTheoDem = roomModels.Sum(m => m.GiaTheoDem);
            combined.PhongDaThanhToanTruoc = string.Join(", ", roomModels.Select(m => m.PhongDaThanhToanTruoc).Where(s => !string.IsNullOrWhiteSpace(s)));
            return combined;
        }

        // HÀM BỔ TRỢ LẤY DỮ LIỆU BẢNG KÊ CHECKOUT & HÓA ĐƠN
        private async Task<HoaDonCheckoutViewModel?> GetHoaDonCheckoutViewModelAsync(int maDatPhong, bool cheDoXuatHoaDon = false, int? maPhong = null)
        {
            var datPhong = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
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

            // ---- Hàm hỗ trợ ----
            static bool DaTraHet(ChiTietDatPhong ct) =>
                ct.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null)
                && !ct.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null && l.ThoiGianTraPhong == null);

            DateTime? TraCuoiCua(ChiTietDatPhong ct) =>
                ct.DanhSachLuuTru.Where(l => l.ThoiGianTraPhong != null).Select(l => l.ThoiGianTraPhong).Max();

            int SoDemCua(ChiTietDatPhong ct)
            {
                var luuTru = ct.DanhSachLuuTru.Where(l => l.ThoiGianNhanPhong != null).ToList();
                if (luuTru.Count == 0) return soDem;
                var nhan = luuTru.Min(l => l.ThoiGianNhanPhong!.Value);
                var tra = luuTru.All(l => l.ThoiGianTraPhong != null)
                    ? luuTru.Max(l => l.ThoiGianTraPhong!.Value)
                    : DateTime.Now;
                return Math.Max(1, (tra.Date - nhan.Date).Days);
            }

            decimal GiaMotDem(ChiTietDatPhong ct) =>
                ct.DonGia > 0 ? ct.DonGia : (ct.Phong?.LoaiPhong?.GiaTheoDem ?? 0);

            decimal GiaDuKien(ChiTietDatPhong ct) =>
                GiaMotDem(ct) * Math.Max(1, (ct.NgayTra.Date - ct.NgayNhan.Date).Days);

            // ---- Các phòng nằm trong hóa đơn ----
            var chiTietTrongHoaDon = datPhong.ChiTietDatPhongs
                .Where(ct => ct.Phong != null)
                .Where(ct => !maPhong.HasValue || ct.MaPhong == maPhong.Value)
                .Where(ct => cacLuuTru.Count == 0 || ct.DanhSachLuuTru.Any(l => l.ThoiGianNhanPhong != null))
                .ToList();

            if (maPhong.HasValue)
            {
                var luuTruPhong = chiTietTrongHoaDon.SelectMany(ct => ct.DanhSachLuuTru)
                    .Where(l => l.ThoiGianNhanPhong != null).ToList();
                if (luuTruPhong.Count > 0)
                {
                    ngayNhan = luuTruPhong.Min(l => l.ThoiGianNhanPhong!.Value);
                    ngayTra = luuTruPhong.Where(l => l.ThoiGianTraPhong != null)
                        .Select(l => l.ThoiGianTraPhong!.Value).DefaultIfEmpty(DateTime.Now).Max();
                }
            }

            // Xuất hóa đơn: chỉ tính các phòng đã trả (nếu có)
            if (cheDoXuatHoaDon)
            {
                var daTraHet = chiTietTrongHoaDon.Where(DaTraHet).ToList();
                if (daTraHet.Count > 0) chiTietTrongHoaDon = daTraHet;
            }

            var danhSachPhong = chiTietTrongHoaDon
                .Select(ct =>
                {
                    var phongDat = ct.Phong!;
                    var gia = GiaMotDem(ct);
                    var dem = SoDemCua(ct);
                    return new PhongHoaDonItemViewModel
                    {
                        SoPhong = phongDat.SoPhong,
                        TenLoaiPhong = phongDat.LoaiPhong?.TenLoaiPhong ?? "Tiêu chuẩn",
                        GiaTheoDem = gia,
                        SoDem = dem,
                        ThanhTien = gia * dem
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

            // ---- Phòng đã thanh toán ở lần trả trước ----
            var phongTraTruoc = new List<ChiTietDatPhong>();
            if (cheDoXuatHoaDon)
            {
                var daTra = chiTietTrongHoaDon
                    .Where(DaTraHet)
                    .Select(ct => new { ct, tra = TraCuoiCua(ct) })
                    .Where(x => x.tra != null)
                    .ToList();

                if (daTra.Count > 1)
                {
                    var lanTraCuoi = daTra.Max(x => x.tra!.Value);
                    phongTraTruoc = daTra
                        .Where(x => (lanTraCuoi - x.tra!.Value).TotalSeconds > 5)
                        .Select(x => x.ct)
                        .ToList();
                }
            }

            // ---- Dịch vụ ----
            var maLuuTruTrongHoaDon = chiTietTrongHoaDon
                .SelectMany(ct => ct.DanhSachLuuTru).Select(l => l.MaLuuTru).ToList();
            var maLuuTruTruoc = phongTraTruoc
                .SelectMany(ct => ct.DanhSachLuuTru).Select(l => l.MaLuuTru).ToList();

            IQueryable<ChiTietDichVu> truyVanDichVu = _context.ChiTietDichVus
                .AsNoTracking()
                .Include(cdv => cdv.DichVu)
                .Where(cdv => _context.LuuTrus.Any(l => l.MaLuuTru == cdv.MaLuuTru
                                                     && l.ChiTietDatPhong != null
                                                     && l.ChiTietDatPhong.MaDatPhong == maDatPhong));

            if (cheDoXuatHoaDon && maLuuTruTrongHoaDon.Count > 0)
                truyVanDichVu = truyVanDichVu.Where(cdv => maLuuTruTrongHoaDon.Contains(cdv.MaLuuTru));

            var dsDichVu = await truyVanDichVu
                .Select(cdv => new ChiTietDichVuItemViewModel
                {
                    TenDichVu = cdv.DichVu != null ? cdv.DichVu.TenDichVu : "Dịch vụ",
                    SoLuong = cdv.SoLuong,
                    DonGia = cdv.DonGia,
                    ThanhTien = cdv.SoLuong * cdv.DonGia
                })
                .ToListAsync();

            decimal dichVuTruoc = 0;
            if (maLuuTruTruoc.Count > 0)
            {
                dichVuTruoc = await _context.ChiTietDichVus.AsNoTracking()
                    .Where(cdv => maLuuTruTruoc.Contains(cdv.MaLuuTru))
                    .SumAsync(cdv => (decimal?)(cdv.SoLuong * cdv.DonGia)) ?? 0;
            }

            // ---- Tiền ----
            decimal tongTienDichVu = dsDichVu.Sum(x => x.ThanhTien);
            decimal tiLeThuePercent = 10;
            decimal tienThue = (tienPhong + tongTienDichVu) * (tiLeThuePercent / 100m);
            decimal tongThanhToan = tienPhong + tongTienDichVu + tienThue;

            decimal tongDuKienCaPhieu = datPhong.ChiTietDatPhongs.Sum(ct => GiaDuKien(ct));
            decimal ChiaCoc(IEnumerable<ChiTietDatPhong> nhom) =>
                tongDuKienCaPhieu > 0
                    ? Math.Round(datPhong.TienCoc * nhom.Sum(ct => GiaDuKien(ct)) / tongDuKienCaPhieu, 0)
                    : 0;

            bool phieuDaDong = datPhong.TrangThai == "DaHoanThanh" || datPhong.TrangThai == "DaThanhToan";
            decimal tienCocApDung = phieuDaDong ? datPhong.TienCoc : ChiaCoc(chiTietTrongHoaDon);

            decimal daThanhToanTruoc = 0;
            if (phongTraTruoc.Count > 0)
            {
                daThanhToanTruoc = Math.Round(
                    (phongTraTruoc.Sum(ct => GiaMotDem(ct) * SoDemCua(ct)) + dichVuTruoc) * (1 + tiLeThuePercent / 100m), 0);
                tienCocApDung = Math.Max(0, tienCocApDung - ChiaCoc(phongTraTruoc));
            }

            decimal soTienConLai = Math.Max(0, tongThanhToan - daThanhToanTruoc - tienCocApDung);

            var paymentSetting = _paymentSettingService.GetSettings();
            string nganHangMa = paymentSetting?.NganHangMa ?? "";
            string soTaiKhoan = paymentSetting?.SoTaiKhoan ?? "";
            string tenChuTaiKhoan = paymentSetting?.TenChuTaiKhoan ?? "";
            string noiDungCK = $"{paymentSetting?.NoiDungPrefix}{datPhong.MaDatPhong}";

            string noiDungMaHoa = Uri.EscapeDataString(noiDungCK);
            string tenTaiKhoanMaHoa = Uri.EscapeDataString(tenChuTaiKhoan);
            string vietQrUrl = $"https://img.vietqr.io/image/{nganHangMa}-{soTaiKhoan}-compact2.png?amount={(long)soTienConLai}&addInfo={noiDungMaHoa}&accountName={tenTaiKhoanMaHoa}";

            return new HoaDonCheckoutViewModel
            {
                MaDatPhong = datPhong.MaDatPhong,
                MaPhong = maPhong,
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
                DaThanhToanTruoc = daThanhToanTruoc,
                PhongDaThanhToanTruoc = string.Join(", ", phongTraTruoc.Select(ct => ct.Phong!.SoPhong)),
                TienCoc = tienCocApDung,
                SoTienConLai = soTienConLai,
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

            var caiDat = await _context.CaiDatHeThongs.FirstOrDefaultAsync();
            ViewBag.TienCocMacDinh = caiDat?.TienCocMacDinh ?? 30;
            ViewBag.MaxDatTruoc = caiDat?.MaxDatTruoc ?? 90;
            ViewBag.GioNhanPhong = caiDat?.GioNhanPhong ?? "14:00";
            ViewBag.GioTraPhong = caiDat?.GioTraPhong ?? "12:00";
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

            var caiDat = await _context.CaiDatHeThongs.FirstOrDefaultAsync();
            ViewBag.TienCocMacDinh = caiDat?.TienCocMacDinh ?? 30;
            ViewBag.MaxDatTruoc = caiDat?.MaxDatTruoc ?? 90;
            ViewBag.GioNhanPhong = caiDat?.GioNhanPhong ?? "14:00";
            ViewBag.GioTraPhong = caiDat?.GioTraPhong ?? "12:00";

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
        public async Task<IActionResult> XuatHoaDon(int id, bool theoDatPhong = false)
        {
            // Ở đây id có thể là MaHD hoặc MaDatPhong, ta tìm theo MaHD trước, nếu không thấy thì tìm theo MaDatPhong
            var hoaDon = theoDatPhong
                ? await _context.HoaDons.Where(h => h.MaDatPhong == id).OrderByDescending(h => h.MaHD).FirstOrDefaultAsync()
                : await _context.HoaDons.FirstOrDefaultAsync(h => h.MaHD == id);
            hoaDon ??= await _context.HoaDons.Where(h => h.MaDatPhong == id).OrderByDescending(h => h.MaHD).FirstOrDefaultAsync();
            if (hoaDon == null || !string.Equals(hoaDon.TrangThai, "DaThanhToan", StringComparison.OrdinalIgnoreCase))
            {
                var maDatPhongCanThanhToan = hoaDon?.MaDatPhong ?? id;
                return RedirectToAction(nameof(Checkout), new { id = maDatPhongCanThanhToan });
            }
            int maDatPhong = hoaDon?.MaDatPhong ?? id;

            var maPhongHoaDon = hoaDon == null ? null : await _context.ChiTietHoaDons.AsNoTracking()
                .Where(c => c.MaHD == hoaDon.MaHD && c.LoaiKhoanThu == "Phong" && c.MaThamChieu.HasValue)
                .Select(c => (int?)_context.ChiTietDatPhongs
                    .Where(ct => ct.MaChiTietDatPhong == c.MaThamChieu.Value)
                    .Select(ct => ct.MaPhong)
                    .FirstOrDefault())
                .FirstOrDefaultAsync();
            var viewModel = await GetHoaDonCheckoutViewModelAsync(maDatPhong, cheDoXuatHoaDon: true, maPhong: maPhongHoaDon);
            if (viewModel == null)
            {
                return NotFound("Không tìm thấy thông tin hóa đơn.");
            }

            ViewBag.MaHoaDon = hoaDon?.MaHD ?? id;
            return View("XuatHoaDon", viewModel);
        }

    }
}
