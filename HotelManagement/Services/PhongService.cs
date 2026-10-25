using HotelManagement.Models;
using HotelManagement.Repositories;

namespace HotelManagement.Services
{
    public class PhongService
    {
        private readonly PhongRepository _repository;

        public PhongService(PhongRepository repository)
        {
            _repository = repository;
        }

        public Task<List<Phong>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public Task<Phong?> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public Task<List<LoaiPhong>> GetLoaiPhongsAsync()
        {
            return _repository.GetLoaiPhongsAsync();
        }

        public async Task<(bool ThanhCong, string ThongBao)> AddAsync(Phong phong)
        {
            phong.SoPhong = phong.SoPhong?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(phong.SoPhong))
            {
                return (false, "Vui lòng nhập số phòng.");
            }

            // Quy uoc: 10x la tang 1, 20x la tang 2...
            if (!int.TryParse(phong.SoPhong, out int so))
            {
                return (false, "Số phòng phải là chữ số.");
            }

            int tang = so / 100;

            if (tang < 1 || tang > 99 || so % 100 == 0)
            {
                return (false, "Số phòng không hợp lệ. Ví dụ: 101, 201, 301.");
            }

            if (await _repository.ExistsSoPhongAsync(phong.SoPhong))
            {
                return (false, "Số phòng này đã tồn tại.");
            }

            var danhSachLoaiPhong = await _repository.GetLoaiPhongsAsync();

            if (!danhSachLoaiPhong.Any(lp => lp.MaLoaiPhong == phong.MaLoaiPhong))
            {
                return (false, "Loại phòng không tồn tại.");
            }

            phong.TrangThai = "Trống";

            await _repository.AddAsync(phong);

            return (true, "Thêm phòng thành công.");
        }

        // Tim va loc phong
        public async Task<IEnumerable<Phong>> SearchAndFilterAsync(string? keyword, int? maLoaiPhong, string? trangThai)
        {
            string? normalizedTrangThai = string.IsNullOrWhiteSpace(trangThai) ? null : trangThai.Trim();

            return await _repository.SearchAndFilterAsync(keyword, maLoaiPhong, normalizedTrangThai);
        }

        // Sua phong
        public async Task<(bool ThanhCong, string ThongBao)> UpdateAsync(Phong phong)
        {
            phong.SoPhong = phong.SoPhong?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(phong.SoPhong))
            {
                return (false, "Vui lòng nhập số phòng.");
            }

            // Kiểm tra định dạng số phòng giống như khi Thêm mới
            if (!int.TryParse(phong.SoPhong, out int so))
            {
                return (false, "Số phòng phải là chữ số.");
            }

            int tang = so / 100;

            if (tang < 1 || tang > 99 || so % 100 == 0)
            {
                return (false, "Số phòng không hợp lệ. Ví dụ: 101, 201, 301.");
            }

            var existingPhong = await _repository.GetByIdAsync(phong.MaPhong);
            if (existingPhong == null)
            {
                return (false, "Phòng không tồn tại.");
            }

            if (await _repository.ExistsSoPhongAsync(phong.SoPhong, phong.MaPhong))
            {
                return (false, "Số phòng này đã tồn tại.");
            }

            var danhSachLoaiPhong = await _repository.GetLoaiPhongsAsync();
            if (!danhSachLoaiPhong.Any(lp => lp.MaLoaiPhong == phong.MaLoaiPhong))
            {
                return (false, "Loại phòng không tồn tại.");
            }
           
            if (string.IsNullOrWhiteSpace(phong.TrangThai))
            {
                phong.TrangThai = existingPhong.TrangThai;
            }

            bool result = await _repository.UpdateAsync(phong);
            if (result)
            {
                return (true, "Cập nhật phòng thành công.");
            }

            return (false, "Có lỗi xảy ra khi cập nhật phòng.");
        }

        // Xoa phong
        public async Task<(bool ThanhCong, string ThongBao)> DeleteAsync(int id)
        {
            bool dangDuocDat = await _repository.KiemTraPhongDangDuocDatAsync(id);
            if (dangDuocDat)
            {
                return (false, "Không thể xóa phòng này vì có khách đang sử dụng hoặc đã đặt.");
            }

            bool ketQua = await _repository.DeleteAsync(id);
            if (ketQua)
            {
                return (true, "Xóa phòng thành công.");
            }
            return (false, "Có lỗi xảy ra khi xóa phòng.");
        }
    }
}