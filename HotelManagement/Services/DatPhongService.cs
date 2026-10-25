
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

                    datPhong.ChiTietDatPhongs.Add(
                        new ChiTietDatPhong
                        {
                            MaPhong = maPhong,
                            NgayNhan = model.NgayNhan.Date,
                            NgayTra = model.NgayTra.Date,
                            DonGia = phong.LoaiPhong!.GiaTheoDem
                        });
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
            var datPhong = await _repository.GetByIdAsync(id);

            if (datPhong == null)
                return (false, "Không tìm thấy phiếu đặt phòng.");

            if (datPhong.TrangThai == "DaHuy")
                return (false, "Phiếu đặt phòng đã được hủy trước đó.");

            if (datPhong.TrangThai != "ChoXacNhan")
                return (false,
                    "Chỉ có thể hủy phiếu đang chờ xác nhận.");

            datPhong.TrangThai = "DaHuy";

            await _repository.UpdateAsync(datPhong);

            return (true, "Hủy phiếu đặt phòng thành công.");
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

                if (phong.TrangThai != "Trong")
                    return $"Phòng {phong.SoPhong} hiện không ở trạng thái trống.";

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
