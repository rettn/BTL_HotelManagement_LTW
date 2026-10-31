
using HotelManagement.Models;
using HotelManagement.Repositories;

namespace HotelManagement.Services
{
    public class PhuCapService
    {
        private readonly PhuCapRepository _repository;
        private readonly NhanVienRepository _nhanVienRepository;

        public PhuCapService(
            PhuCapRepository repository,
            NhanVienRepository nhanVienRepository)
        {
            _repository = repository;
            _nhanVienRepository = nhanVienRepository;
        }

        public Task<List<PhuCap>> GetByMonthAsync(int thang, int nam)
        {
            ValidateMonthYear(thang, nam);
            return _repository.GetByMonthAsync(thang, nam);
        }

        public Task<PhuCap?> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message)> AddAsync(
            PhuCap phuCap)
        {
            var validation = await ValidateAsync(phuCap);

            if (validation != null)
                return (false, validation);

            phuCap.TenPhuCap = phuCap.TenPhuCap.Trim();
            phuCap.LyDo = phuCap.LyDo?.Trim();
            phuCap.NgayTao = DateTime.Now;

            await _repository.AddAsync(phuCap);

            return (true, "Them phu cap thanh cong.");
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            PhuCap phuCap)
        {
            var existing = await _repository.GetByIdAsync(
                phuCap.MaPhuCap);

            if (existing == null)
                return (false, "Khong tim thay khoan phu cap.");

            var validation = await ValidateAsync(phuCap);

            if (validation != null)
                return (false, validation);

            existing.MaNV = phuCap.MaNV;
            existing.Thang = phuCap.Thang;
            existing.Nam = phuCap.Nam;
            existing.TenPhuCap = phuCap.TenPhuCap.Trim();
            existing.SoTien = phuCap.SoTien;
            existing.LyDo = phuCap.LyDo?.Trim();

            await _repository.UpdateAsync(existing);

            return (true, "Cap nhat phu cap thanh cong.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var phuCap = await _repository.GetByIdAsync(id);

            if (phuCap == null)
                return (false, "Khong tim thay khoan phu cap.");

            await _repository.DeleteAsync(phuCap);

            return (true, "Xoa phu cap thanh cong.");
        }

        private async Task<string?> ValidateAsync(PhuCap phuCap)
        {
            ValidateMonthYear(phuCap.Thang, phuCap.Nam);

            if (!await _nhanVienRepository.ExistsAsync(phuCap.MaNV))
                return "Nhan vien khong ton tai.";

            if (string.IsNullOrWhiteSpace(phuCap.TenPhuCap))
                return "Ten phu cap khong duoc de trong.";

            if (phuCap.SoTien <= 0)
                return "So tien phu cap phai lon hon 0.";

            return null;
        }

        private static void ValidateMonthYear(int thang, int nam)
        {
            if (thang < 1 || thang > 12 || nam < 2000 || nam > 2100)
                throw new ArgumentException("Thang hoac nam khong hop le.");
        }
    }
}
