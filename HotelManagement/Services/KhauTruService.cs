
using HotelManagement.Models;
using HotelManagement.Repositories;

namespace HotelManagement.Services
{
    public class KhauTruService
    {
        private readonly KhauTruRepository _repository;
        private readonly NhanVienRepository _nhanVienRepository;

        public KhauTruService(
            KhauTruRepository repository,
            NhanVienRepository nhanVienRepository)
        {
            _repository = repository;
            _nhanVienRepository = nhanVienRepository;
        }

        public Task<List<KhauTru>> GetByMonthAsync(int thang, int nam)
        {
            ValidateMonthYear(thang, nam);
            return _repository.GetByMonthAsync(thang, nam);
        }

        public Task<KhauTru?> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message)> AddAsync(
            KhauTru khauTru)
        {
            var validation = await ValidateAsync(khauTru);

            if (validation != null)
                return (false, validation);

            khauTru.TenKhoanTru = khauTru.TenKhoanTru.Trim();
            khauTru.LyDo = khauTru.LyDo?.Trim();
            khauTru.NgayTao = DateTime.Now;

            await _repository.AddAsync(khauTru);

            return (true, "Them khoan khau tru thanh cong.");
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            KhauTru khauTru)
        {
            var existing = await _repository.GetByIdAsync(
                khauTru.MaKhauTru);

            if (existing == null)
                return (false, "Khong tim thay khoan khau tru.");

            var validation = await ValidateAsync(khauTru);

            if (validation != null)
                return (false, validation);

            existing.MaNV = khauTru.MaNV;
            existing.Thang = khauTru.Thang;
            existing.Nam = khauTru.Nam;
            existing.TenKhoanTru = khauTru.TenKhoanTru.Trim();
            existing.SoTien = khauTru.SoTien;
            existing.LyDo = khauTru.LyDo?.Trim();

            await _repository.UpdateAsync(existing);

            return (true, "Cap nhat khoan khau tru thanh cong.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var khauTru = await _repository.GetByIdAsync(id);

            if (khauTru == null)
                return (false, "Khong tim thay khoan khau tru.");

            await _repository.DeleteAsync(khauTru);

            return (true, "Xoa khoan khau tru thanh cong.");
        }

        private async Task<string?> ValidateAsync(KhauTru khauTru)
        {
            ValidateMonthYear(khauTru.Thang, khauTru.Nam);

            if (!await _nhanVienRepository.ExistsAsync(khauTru.MaNV))
                return "Nhan vien khong ton tai.";

            if (string.IsNullOrWhiteSpace(khauTru.TenKhoanTru))
                return "Ten khoan khau tru khong duoc de trong.";

            if (khauTru.SoTien <= 0)
                return "So tien khau tru phai lon hon 0.";

            return null;
        }

        private static void ValidateMonthYear(int thang, int nam)
        {
            if (thang < 1 || thang > 12 || nam < 2000 || nam > 2100)
                throw new ArgumentException("Thang hoac nam khong hop le.");
        }
    }
}
