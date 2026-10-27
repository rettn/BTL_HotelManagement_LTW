
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class LapHoaDonService
    {
        private readonly ApplicationDbContext _context;

        public LapHoaDonService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lập hóa đơn cho lượt lưu trú đã trả phòng
        public async Task<(bool Success, string Message)> LapHoaDonAsync(
            LapHoaDonViewModel model)
        {
            if (model.GiamGia < 0)
                return (false, "Giảm giá không được âm.");

            var luuTru = await _context.LuuTrus
                .Include(l => l.ChiTietDatPhong)
                    .ThenInclude(c => c!.DatPhong)
                .Include(l => l.ChiTietDatPhong)
                    .ThenInclude(c => c!.Phong)
                .Include(l => l.ChiTietDichVus)
                    .ThenInclude(d => d.DichVu)
                .FirstOrDefaultAsync(l => l.MaLuuTru == model.MaLuuTru);

            if (luuTru == null)
                return (false, "Không tìm thấy lượt lưu trú.");

            if (luuTru.ThoiGianNhanPhong == null)
                return (false, "Lượt lưu trú chưa được nhận phòng.");

            if (luuTru.ThoiGianTraPhong == null)
                return (false, "Khách chưa trả phòng.");

            if (luuTru.ChiTietDatPhong == null ||
                luuTru.ChiTietDatPhong.DatPhong == null)
                return (false, "Không tìm thấy thông tin đặt phòng.");

            int maDatPhong = luuTru.ChiTietDatPhong.MaDatPhong;

            // Kiểm tra hóa đơn đã tồn tại
            bool daCoHoaDon = await _context.HoaDons
                .AnyAsync(h => h.MaDatPhong == maDatPhong);

            if (daCoHoaDon)
                return (false, "Đặt phòng này đã có hóa đơn.");

            // Tính số đêm, tối thiểu 1 đêm
            DateTime ngayNhan = luuTru.ThoiGianNhanPhong.Value;
            DateTime ngayTra = luuTru.ThoiGianTraPhong.Value;

            if (ngayTra <= ngayNhan)
                return (false, "Thời gian trả phòng không hợp lệ.");

            int soDem = Math.Max(
                1,
                (int)Math.Ceiling((ngayTra - ngayNhan).TotalDays));

            decimal donGiaPhong = luuTru.ChiTietDatPhong.DonGia;
            decimal tienPhong = soDem * donGiaPhong;

            // Tính dịch vụ theo đơn giá đã lưu lúc sử dụng
            decimal tienDichVu = luuTru.ChiTietDichVus
                .Sum(d => d.SoLuong * d.DonGia);

            decimal giamGia = model.GiamGia;

            if (giamGia > tienPhong + tienDichVu)
                return (false, "Giảm giá không được vượt quá tổng tiền.");

            decimal tongTien = tienPhong + tienDichVu - giamGia;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var hoaDon = new HoaDon
                {
                    MaDatPhong = maDatPhong,
                    NgayLap = DateTime.Now,
                    TienPhong = tienPhong,
                    TienDichVu = tienDichVu,
                    GiamGia = giamGia,
                    TongTien = tongTien,
                    TrangThai = "ChuaThanhToan"
                };

                _context.HoaDons.Add(hoaDon);
                await _context.SaveChangesAsync();

                // Thêm khoản tiền phòng
                hoaDon.ChiTietHoaDons.Add(new ChiTietHoaDon
                {
                    TenKhoanThu = "Tiền phòng",
                    LoaiKhoanThu = "Phong",
                    MaThamChieu = luuTru.ChiTietDatPhong.MaChiTietDatPhong,
                    SoLuong = soDem,
                    DonGia = donGiaPhong,
                    ThanhTien = tienPhong
                });

                // Thêm từng khoản dịch vụ
                foreach (var dichVu in luuTru.ChiTietDichVus)
                {
                    hoaDon.ChiTietHoaDons.Add(new ChiTietHoaDon
                    {
                        TenKhoanThu = dichVu.DichVu?.TenDichVu
                            ?? $"Dịch vụ #{dichVu.MaDichVu}",
                        LoaiKhoanThu = "DichVu",
                        MaThamChieu = dichVu.MaDichVu,
                        SoLuong = dichVu.SoLuong,
                        DonGia = dichVu.DonGia,
                        ThanhTien = dichVu.SoLuong * dichVu.DonGia
                    });
                }

                if (giamGia > 0)
                {
                    hoaDon.ChiTietHoaDons.Add(new ChiTietHoaDon
                    {
                        TenKhoanThu = "Giảm giá",
                        LoaiKhoanThu = "GiamGia",
                        SoLuong = 1,
                        DonGia = -giamGia,
                        ThanhTien = -giamGia
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Lập hóa đơn thành công.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<LapHoaDonViewModel?> XemTruocAsync(
            int maLuuTru,
            decimal giamGia)
        {
            if (giamGia < 0)
                return null;

            var luuTru = await _context.LuuTrus
                .Include(l => l.ChiTietDatPhong)
                    .ThenInclude(c => c!.DatPhong)
                        .ThenInclude(d => d!.KhachHang)
                .Include(l => l.ChiTietDatPhong)
                    .ThenInclude(c => c!.Phong)
                .Include(l => l.ChiTietDichVus)
                .FirstOrDefaultAsync(l => l.MaLuuTru == maLuuTru);

            if (luuTru?.ThoiGianNhanPhong == null ||
                luuTru.ThoiGianTraPhong == null ||
                luuTru.ChiTietDatPhong == null)
            {
                return null;
            }

            // Không cho xem trước nếu đặt phòng đã có hóa đơn
            int maDatPhong = luuTru.ChiTietDatPhong.MaDatPhong;

            bool daCoHoaDon = await _context.HoaDons
                .AnyAsync(h => h.MaDatPhong == maDatPhong);

            if (daCoHoaDon)
            {
                return null;
            }
            var ngayNhan = luuTru.ThoiGianNhanPhong.Value;
            var ngayTra = luuTru.ThoiGianTraPhong.Value;

            if (ngayTra <= ngayNhan)
                return null;

            int soDem = Math.Max(
                1,
                (int)Math.Ceiling((ngayTra - ngayNhan).TotalDays));

            decimal tienPhong = soDem * luuTru.ChiTietDatPhong.DonGia;

            decimal tienDichVu = luuTru.ChiTietDichVus
                .Sum(d => d.SoLuong * d.DonGia);

            if (giamGia > tienPhong + tienDichVu)
                return null;

            return new LapHoaDonViewModel
            {
                MaLuuTru = luuTru.MaLuuTru,
                MaDatPhong = luuTru.ChiTietDatPhong.MaDatPhong,
                MaChiTietDatPhong =
                    luuTru.ChiTietDatPhong.MaChiTietDatPhong,

                TenKhachHang =
                    luuTru.ChiTietDatPhong.DatPhong?.KhachHang?.HoTen
                    ?? "Chưa có thông tin",

                SoPhong =
                    luuTru.ChiTietDatPhong.Phong?.SoPhong
                    ?? "Chưa xác định",

                ThoiGianNhanPhong = ngayNhan,
                ThoiGianTraPhong = ngayTra,
                SoDem = soDem,

                DonGiaPhong = luuTru.ChiTietDatPhong.DonGia,
                TienPhong = tienPhong,
                TienDichVu = tienDichVu,
                GiamGia = giamGia
            };


        }

        
    }
}
