
using HotelManagement.Models;
using HotelManagement.Repositories;

namespace HotelManagement.Services
{
    public class ThuongService
    {
        private readonly ThuongRepository _repository;
        private readonly NhanVienRepository _nhanVienRepository;

        public ThuongService(
            ThuongRepository repository,
            NhanVienRepository nhanVienRepository)
        {
            _repository = repository;
            _nhanVienRepository = nhanVienRepository;
        }

        public Task<List<Thuong>> GetByMonthAsync(int thang, int nam)
        {
            ValidateMonthYear(thang, nam);
            return _repository.GetByMonthAsync(thang, nam);
        }

        public Task<Thuong?> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message)> AddAsync(
            Thuong thuong)
        {
            var validation = await ValidateAsync(thuong);

            if (validation != null)
                return (false, validation);

            thuong.TenThuong = thuong.TenThuong.Trim();
            thuong.LyDo = thuong.LyDo?.Trim();
            thuong.NgayTao = DateTime.Now;

            await _repository.AddAsync(thuong);

            return (true, "Them khoan thuong thanh cong.");
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            Thuong thuong)
        {
            var existing = await _repository.GetByIdAsync(
                thuong.MaThuong);

            if (existing == null)
                return (false, "Khong tim thay khoan thuong.");

            var validation = await ValidateAsync(thuong);

            if (validation != null)
                return (false, validation);

            existing.MaNV = thuong.MaNV;
            existing.Thang = thuong.Thang;
            existing.Nam = thuong.Nam;
            existing.TenThuong = thuong.TenThuong.Trim();
            existing.SoTien = thuong.SoTien;
            existing.LyDo = thuong.LyDo?.Trim();

            await _repository.UpdateAsync(existing);

            return (true, "Cap nhat khoan thuong thanh cong.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var thuong = await _repository.GetByIdAsync(id);

            if (thuong == null)
                return (false, "Khong tim thay khoan thuong.");

            await _repository.DeleteAsync(thuong);

            return (true, "Xoa khoan thuong thanh cong.");
        }

        private async Task<string?> ValidateAsync(Thuong thuong)
        {
            ValidateMonthYear(thuong.Thang, thuong.Nam);

            if (!await _nhanVienRepository.ExistsAsync(thuong.MaNV))
                return "Nhan vien khong ton tai.";

            if (string.IsNullOrWhiteSpace(thuong.TenThuong))
                return "Ten khoan thuong khong duoc de trong.";

            if (thuong.SoTien <= 0)
                return "So tien thuong phai lon hon 0.";

            return null;
        }

        private static void ValidateMonthYear(int thang, int nam)
        {
            if (thang < 1 || thang > 12 || nam < 2000 || nam > 2100)
                throw new ArgumentException("Thang hoac nam khong hop le.");
        }
    }
}
