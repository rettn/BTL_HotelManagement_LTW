
using HotelManagement.Models;
using HotelManagement.Repositories;

namespace HotelManagement.Services
{
    public class ChucVuService
    {
        private readonly ChucVuRepository _repository;

        public ChucVuService(ChucVuRepository repository)
        {
            _repository = repository;
        }

        public Task<List<ChucVu>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public Task<ChucVu?> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(ChucVu chucVu)
        {
            chucVu.TenChucVu = chucVu.TenChucVu.Trim();
            chucVu.MoTa = chucVu.MoTa?.Trim();

            if (await _repository.ExistsByNameAsync(chucVu.TenChucVu))
                throw new ArgumentException("Ten chuc vu da ton tai.");

            await _repository.AddAsync(chucVu);
        }

        public async Task<bool> UpdateAsync(ChucVu chucVu)
        {
            var existing = await _repository.GetByIdAsync(
                chucVu.MaChucVu);

            if (existing == null)
                return false;

            chucVu.TenChucVu = chucVu.TenChucVu.Trim();

            if (await _repository.ExistsByNameAsync(
                chucVu.TenChucVu, chucVu.MaChucVu))
            {
                throw new ArgumentException("Ten chuc vu da ton tai.");
            }

            // Neu doi ten chuc vu, cap nhat cac nhan vien dang su dung.
            var oldName = existing.TenChucVu;

            existing.TenChucVu = chucVu.TenChucVu;
            existing.MoTa = chucVu.MoTa?.Trim();
            existing.TrangThai = chucVu.TrangThai;

            // Cap nhat ten chuc vu trong bang NhanVien.
            var nhanViens = await _repository.GetNhanViensByChucVuAsync(
                oldName);

            foreach (var nv in nhanViens)
                nv.ChucVu = existing.TenChucVu;

            await _repository.UpdateAsync(existing);
            return true;
        }

        public Task<bool> DeleteAsync(int id)
        {
            return _repository.DeleteAsync(id);
        }
    }
}
