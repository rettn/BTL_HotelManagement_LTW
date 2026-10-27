
using HotelManagement.Models;
using HotelManagement.Repositories;

namespace HotelManagement.Services
{
    public class KhachHangService
    {
        private readonly KhachHangRepository _repository;

        public KhachHangService(KhachHangRepository repository)
        {
            _repository = repository;
        }

        public Task<List<KhachHang>> GetAllAsync(string? keyword = null)
        {
            return _repository.GetAllAsync(keyword);
        }

        public Task<KhachHang?> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message)> CreateAsync(
            KhachHang khachHang)
        {
            var validation = await ValidateAsync(khachHang);

            if (validation != null)
                return (false, validation);

            khachHang.MaKH = 0;
            khachHang.NgayTao = DateTime.Now;

            await _repository.AddAsync(khachHang);

            return (true, "Thêm khách hàng thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            KhachHang khachHang)
        {
            var existing = await _repository.GetByIdAsync(khachHang.MaKH);

            if (existing == null)
                return (false, "Không tìm thấy khách hàng.");

            var validation = await ValidateAsync(
                khachHang, khachHang.MaKH);

            if (validation != null)
                return (false, validation);

            existing.HoTen = khachHang.HoTen.Trim();
            existing.GioiTinh = khachHang.GioiTinh;
            existing.NgaySinh = khachHang.NgaySinh;
            existing.SoDienThoai = khachHang.SoDienThoai;
            existing.Email = khachHang.Email;
            existing.DiaChi = khachHang.DiaChi;
            existing.CCCD = khachHang.CCCD;

            await _repository.UpdateAsync(existing);

            return (true, "Cập nhật khách hàng thành công.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var khachHang = await _repository.GetByIdAsync(id);

            if (khachHang == null)
                return (false, "Không tìm thấy khách hàng.");

            if (await _repository.HasBookingAsync(id))
            {
                return (false,
                    "Khách hàng đã có phiếu đặt phòng, không thể xóa.");
            }

            await _repository.DeleteAsync(khachHang);

            return (true, "Xóa khách hàng thành công.");
        }

        private async Task<string?> ValidateAsync(
            KhachHang khachHang, int? excludeId = null)
        {
            khachHang.HoTen = khachHang.HoTen?.Trim() ?? "";
            khachHang.SoDienThoai = khachHang.SoDienThoai?.Trim();
            khachHang.Email = string.IsNullOrWhiteSpace(khachHang.Email)
                ? null
                : khachHang.Email.Trim();
            khachHang.CCCD = khachHang.CCCD?.Trim();
            khachHang.DiaChi = khachHang.DiaChi?.Trim();

            if (string.IsNullOrWhiteSpace(khachHang.HoTen))
                return "Vui lòng nhập họ tên khách hàng.";

            if (!string.IsNullOrWhiteSpace(khachHang.SoDienThoai) &&
                await _repository.IsPhoneUsedAsync(
                    khachHang.SoDienThoai.Trim(), excludeId))
                return "Số điện thoại đã được sử dụng.";

            if (!string.IsNullOrWhiteSpace(khachHang.Email) &&
                await _repository.IsEmailUsedAsync(
                    khachHang.Email.Trim(), excludeId))
                return "Email đã được sử dụng.";

            if (!string.IsNullOrWhiteSpace(khachHang.CCCD) &&
                await _repository.IsCCCDUsedAsync(
                    khachHang.CCCD.Trim(), excludeId))
                return "CCCD đã được sử dụng.";

            if (khachHang.NgaySinh.HasValue &&
                khachHang.NgaySinh.Value.Date > DateTime.Today)
                return "Ngày sinh không được ở tương lai.";

            return null;
        }

        public Task<KhachHang?> GetByIdWithBookingsAsync(int id)
        {
            return _repository.GetByIdWithBookingsAsync(id);
        }
    }
}
