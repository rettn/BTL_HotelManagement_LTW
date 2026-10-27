
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Repositories;
using HotelManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class DatPhongService
    {
        private readonly DatPhongRepository _repository;
        private readonly ApplicationDbContext _context;

        public DatPhongService(
            DatPhongRepository repository,
            ApplicationDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public Task<List<DatPhong>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public Task<DatPhong?> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message)> CreateAsync(
            DatPhongViewModel model)
        {
            var validation = await ValidateAsync(model);

            if (validation != null)
                return (false, validation);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var datPhong = new DatPhong
                {
                    MaKH = model.MaKH,
                    NgayDat = DateTime.Now,
                    NgayNhanDuKien = model.NgayNhan.Date,
                    NgayTraDuKien = model.NgayTra.Date,
                    TienCoc = model.TienCoc,
                    GhiChu = model.GhiChu?.Trim(),
                    TrangThai = "ChoXacNhan"
                };

                foreach (var maPhong in model.MaPhongs.Distinct())
                {
                    var phong = await _context.Phongs
                        .Include(p => p.LoaiPhong)
                        .FirstAsync(p => p.MaPhong == maPhong);

                    datPhong.ChiTietDatPhongs.Add(new ChiTietDatPhong
                    {
                        MaPhong = maPhong,
                        NgayNhan = model.NgayNhan.Date,
                        NgayTra = model.NgayTra.Date,
                        DonGia = phong.LoaiPhong!.GiaTheoDem
                    });

                    phong.TrangThai = "DaDat";
                }

                _context.DatPhongs.Add(datPhong);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Tạo phiếu đặt phòng thành công.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "Không thể tạo phiếu đặt phòng.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            DatPhongViewModel model)
        {
            var datPhong = await _repository.GetByIdAsync(
                model.MaDatPhong);

            if (await _repository.CoLuuTruAsync(model.MaDatPhong))
            {
                return (false,
                    "Phiếu đã phát sinh lưu trú, không thể thay đổi phòng hoặc ngày đặt.");
            }

            if (await _repository.CoHoaDonAsync(model.MaDatPhong))
            {
                return (false,
                    "Phiếu đã phát sinh hóa đơn, không thể thay đổi thông tin đặt phòng.");
            }

            if (datPhong == null)
                return (false, "Không tìm thấy phiếu đặt phòng.");

            if (datPhong.TrangThai == "DaHuy")
                return (false, "Không thể sửa phiếu đã hủy.");

            if (datPhong.TrangThai != "ChoXacNhan")
                return (false,
                    "Chỉ có thể sửa phiếu đang chờ xác nhận.");

            var validation = await ValidateAsync(
                model, model.MaDatPhong);

            if (validation != null)
                return (false, validation);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                datPhong.MaKH = model.MaKH;
                datPhong.NgayNhanDuKien = model.NgayNhan.Date;
                datPhong.NgayTraDuKien = model.NgayTra.Date;
                datPhong.TienCoc = model.TienCoc;
                datPhong.GhiChu = model.GhiChu?.Trim();

                _context.ChiTietDatPhongs.RemoveRange(
                    datPhong.ChiTietDatPhongs);

                foreach (var maPhong in model.MaPhongs.Distinct())
                {
                    var phong = await _context.Phongs
                        .Include(p => p.LoaiPhong)
                        .FirstAsync(p => p.MaPhong == maPhong);

                    datPhong.ChiTietDatPhongs.Add(
                        new ChiTietDatPhong
                        {
                            MaDatPhong = datPhong.MaDatPhong,
                            MaPhong = maPhong,
                            NgayNhan = model.NgayNhan.Date,
                            NgayTra = model.NgayTra.Date,
                            DonGia = phong.LoaiPhong!.GiaTheoDem
                        });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Cập nhật phiếu đặt phòng thành công.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "Không thể cập nhật phiếu đặt phòng.");
            }
        }

        public async Task<(bool Success, string Message)> CancelAsync(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var datPhong = await _context.DatPhongs
                    .Include(d => d.ChiTietDatPhongs)
                        .ThenInclude(c => c.Phong)
                    .FirstOrDefaultAsync(d => d.MaDatPhong == id);

                if (datPhong == null)
                    return (false, "Không tìm thấy phiếu đặt phòng.");

                if (datPhong.TrangThai == "DaHuy")
                    return (false, "Phiếu đặt phòng đã được hủy trước đó.");

                if (datPhong.TrangThai != "ChoXacNhan")
                    return (false, "Chỉ có thể hủy phiếu đang chờ xác nhận.");

                var maPhongList = datPhong.ChiTietDatPhongs
                    .Select(c => c.MaPhong)
                    .Distinct()
                    .ToList();

                datPhong.TrangThai = "DaHuy";

                await _context.SaveChangesAsync();

                foreach (var maPhong in maPhongList)
                {
                    var phong = await _context.Phongs
                        .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

                    if (phong == null)
                        continue;

                    var dangCoLuuTru = await _context.LuuTrus
                        .AnyAsync(l =>
                            l.ChiTietDatPhong != null &&
                            l.ChiTietDatPhong.MaPhong == maPhong &&
                            l.ThoiGianTraPhong == null);

                    var conDatPhong = await _context.ChiTietDatPhongs
                        .AnyAsync(c =>
                            c.MaPhong == maPhong &&
                            c.MaDatPhong != id &&
                            c.DatPhong != null &&
                            c.DatPhong.TrangThai != "DaHuy" &&
                            c.DatPhong.TrangThai != "DaThanhToan" &&
                            c.DatPhong.TrangThai != "DaHoanThanh");

                    if (!dangCoLuuTru && !conDatPhong)
                        phong.TrangThai = "Trong";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Hủy phiếu đặt phòng thành công.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "Không thể hủy phiếu đặt phòng.");
            }
        }


        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();
            try
            {
                var datPhong = await _context.DatPhongs
                    .Include(d => d.ChiTietDatPhongs)
                    .FirstOrDefaultAsync(d => d.MaDatPhong == id);

                if (datPhong == null)
                    return (false, "Không tìm thấy phiếu đặt phòng.");

                if (datPhong.TrangThai != "DaHuy")
                    return (false, "Chỉ có thể xóa phiếu đã hủy.");

                if (await _repository.CoLuuTruAsync(id))
                    return (false, "Phiếu đã phát sinh lưu trú, không thể xóa.");

                if (await _repository.CoHoaDonAsync(id))
                    return (false, "Phiếu đã phát sinh hóa đơn, không thể xóa.");

                _context.ChiTietDatPhongs.RemoveRange(datPhong.ChiTietDatPhongs);
                _context.DatPhongs.Remove(datPhong);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, "Xóa phiếu đặt phòng thành công.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "Không thể xóa phiếu đặt phòng.");
            }
        }

        public async Task<(bool Success, string Message)> CheckInAsync(
        int maDatPhong,
        int maPhong)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var chiTiet = await _context.ChiTietDatPhongs
                    .Include(c => c.DatPhong)
                    .Include(c => c.Phong)
                    .FirstOrDefaultAsync(c =>
                        c.MaDatPhong == maDatPhong &&
                        c.MaPhong == maPhong);

                if (chiTiet == null || chiTiet.DatPhong == null)
                    return (false, "Không tìm thấy chi tiết đặt phòng.");

                if (chiTiet.DatPhong.TrangThai == "DaHuy")
                    return (false, "Phiếu đặt phòng đã bị hủy.");

                if (chiTiet.Phong == null)
                    return (false, "Không tìm thấy phòng.");

                if (chiTiet.Phong.TrangThai != "DaDat" &&
                    chiTiet.Phong.TrangThai != "Đã đặt")
                {
                    return (false, "Phòng không ở trạng thái đã đặt.");
                }

                var daNhan = await _context.LuuTrus.AnyAsync(l =>
                    l.MaChiTietDatPhong == chiTiet.MaChiTietDatPhong &&
                    l.ThoiGianTraPhong == null);

                if (daNhan)
                    return (false, "Phòng này đã được nhận.");

                _context.LuuTrus.Add(new LuuTru
                {
                    MaChiTietDatPhong = chiTiet.MaChiTietDatPhong,
                    ThoiGianNhanPhong = DateTime.Now
                });

                chiTiet.Phong.TrangThai = "DangSuDung";
                chiTiet.DatPhong.TrangThai = "DangLuuTru";

                // Chi tiet da nhan phong duoc theo doi qua LuuTru.
                // Chi tiet cac phong chua nhan van giu nguyen.
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Nhận phòng thành công.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "Không thể nhận phòng.");
            }
        }

        public async Task<(bool Success, string Message)> CheckOutAsync(
    int maDatPhong,
    int maPhong)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();
            try
            {
                var chiTiet = await _context.ChiTietDatPhongs
                    .Include(c => c.DatPhong)
                    .Include(c => c.Phong)
                    .FirstOrDefaultAsync(c =>
                        c.MaDatPhong == maDatPhong &&
                        c.MaPhong == maPhong);

                if (chiTiet == null || chiTiet.DatPhong == null || chiTiet.Phong == null)
                    return (false, "Không tìm thấy chi tiết đặt phòng.");

                var luuTru = await _context.LuuTrus
                    .Where(l =>
                        l.MaChiTietDatPhong == chiTiet.MaChiTietDatPhong &&
                        l.ThoiGianNhanPhong != null &&
                        l.ThoiGianTraPhong == null)
                    .OrderByDescending(l => l.ThoiGianNhanPhong)
                    .FirstOrDefaultAsync();

                if (luuTru == null)
                    return (false, "Phòng này chưa nhận hoặc đã trả phòng.");

                luuTru.ThoiGianTraPhong = DateTime.Now;
                chiTiet.Phong.TrangThai = "DangDon";
                await _context.SaveChangesAsync();

                var conPhongChuaTra = await _context.ChiTietDatPhongs
                    .Where(c => c.MaDatPhong == maDatPhong)
                    .AnyAsync(c => !c.DanhSachLuuTru
                        .Any(l => l.ThoiGianTraPhong != null));

                if (!conPhongChuaTra)
                {
                    chiTiet.DatPhong.TrangThai = "DaHoanThanh";
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return (true, "Trả phòng thành công. Phòng chuyển sang đang dọn dẹp.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "Không thể trả phòng.");
            }
        }

        private async Task<string?> ValidateAsync(
            DatPhongViewModel model,
            int? boQuaMaDatPhong = null)
        {
            if (model.NgayTra.Date <= model.NgayNhan.Date)
                return "Ngày trả phòng phải sau ngày nhận phòng.";

            if (model.NgayNhan.Date < DateTime.Today)
                return "Ngày nhận phòng không được ở quá khứ.";

            if (model.TienCoc < 0)
                return "Tiền cọc không được âm.";

            if (!await _repository.KhachHangTonTaiAsync(model.MaKH))
                return "Khách hàng không tồn tại.";

            var maPhongs = model.MaPhongs
                .Distinct()
                .ToList();

            if (maPhongs.Count == 0)
                return "Vui lòng chọn ít nhất một phòng.";

            foreach (var maPhong in maPhongs)
            {
                var phong = await _context.Phongs
                    .Include(p => p.LoaiPhong)
                    .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

                if (phong == null)
                    return $"Phòng có mã {maPhong} không tồn tại.";

                if (phong.LoaiPhong == null)
                    return $"Phòng {phong.SoPhong} chưa có loại phòng hợp lệ.";

                var trangThaiPhong = phong.TrangThai?.Trim();

                if (trangThaiPhong != "Trong" &&
                    trangThaiPhong != "Trống")
                {
                    return $"Phòng {phong.SoPhong} hiện không ở trạng thái trống.";
                }

                if (await _repository.PhongBiTrungLichAsync(
                    maPhong,
                    model.NgayNhan.Date,
                    model.NgayTra.Date,
                    boQuaMaDatPhong))
                {
                    return $"Phòng {phong.SoPhong} đã được đặt trong khoảng thời gian này.";
                }
            }

            return null;
        }
    }
}
