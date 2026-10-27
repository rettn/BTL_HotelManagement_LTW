
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class GhiNhanDichVuService
    {
        private readonly ApplicationDbContext _context;

        public GhiNhanDichVuService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy các lượt lưu trú chưa trả phòng
        public async Task<List<SelectListItem>> GetDanhSachLuuTruAsync()
        {
            return await _context.LuuTrus
                .AsNoTracking()
                .Where(l =>
                    l.ThoiGianNhanPhong != null &&
                    l.ThoiGianTraPhong == null)
                .Select(l => new SelectListItem
                {
                    Value = l.MaLuuTru.ToString(),
                    Text = "Lưu trú #" + l.MaLuuTru
                        + " - Phòng "
                        + l.ChiTietDatPhong!.Phong!.SoPhong
                        + " - "
                        + l.ChiTietDatPhong.DatPhong!.KhachHang!.HoTen
                })
                .ToListAsync();
        }

        // Chỉ lấy dịch vụ đang hoạt động
        public async Task<List<SelectListItem>> GetDanhSachDichVuAsync()
        {
            return await _context.DichVus
                .AsNoTracking()
                .Where(d => d.TrangThai)
                .OrderBy(d => d.TenDichVu)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.TenDichVu + " - "
                        + d.Gia.ToString("N0") + " VNĐ"
                })
                .ToListAsync();
        }

        // Ghi nhận dịch vụ khách đã sử dụng
        public async Task<(bool Success, string Message)> CreateAsync(
            GhiNhanDichVuViewModel model)
        {
            if (model.SoLuong < 1 || model.SoLuong > 1000)
                return (false, "Số lượng phải từ 1 đến 1000.");

            var luuTru = await _context.LuuTrus
                .FirstOrDefaultAsync(l =>
                    l.MaLuuTru == model.MaLuuTru &&
                    l.ThoiGianNhanPhong != null &&
                    l.ThoiGianTraPhong == null);

            if (luuTru == null)
                return (false,
                    "Không tìm thấy lượt lưu trú đang hoạt động.");

            var dichVu = await _context.DichVus
                .FirstOrDefaultAsync(d =>
                    d.Id == model.MaDichVu &&
                    d.TrangThai);

            if (dichVu == null)
                return (false, "Dịch vụ không tồn tại hoặc đã ngừng hoạt động.");

            var chiTiet = new ChiTietDichVu
            {
                MaLuuTru = luuTru.MaLuuTru,
                MaDichVu = dichVu.Id,
                SoLuong = model.SoLuong,
                DonGia = dichVu.Gia,
                ThoiGianSuDung = DateTime.Now,
                GhiChu = string.IsNullOrWhiteSpace(model.GhiChu)
                    ? null
                    : model.GhiChu.Trim()
            };

            _context.ChiTietDichVus.Add(chiTiet);
            await _context.SaveChangesAsync();

            return (true, "Ghi nhận dịch vụ thành công.");
        }
    }
}
