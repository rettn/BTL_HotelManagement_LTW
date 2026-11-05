
using HotelManagement.Models;
using HotelManagement.Repositories;
using HotelManagement.ViewModels;

namespace HotelManagement.Services
{
    public class NhanVienService
    {
        private readonly NhanVienRepository _repository;

        public NhanVienService(NhanVienRepository repository)
        {
            _repository = repository;
        }

        public Task<List<NhanVien>> GetAllAsync(string? search = null)
        {
            return _repository.GetAllAsync(search);
        }

        public Task<PagedResult<NhanVien>> GetPageAsync(string? search, int page, int pageSize) =>
            _repository.GetPageAsync(search, page, pageSize);

        public Task<NhanVien?> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(NhanVien nhanVien)
        {
            nhanVien.HoTen = nhanVien.HoTen.Trim();
            nhanVien.ChucVu = nhanVien.ChucVu.Trim();

            if (nhanVien.LuongCoBan < 0)
                throw new ArgumentException("Luong co ban khong duoc am.");

            await _repository.AddAsync(nhanVien);
        }

        public async Task<bool> UpdateAsync(NhanVien nhanVien)
        {
            var existing = await _repository.GetByIdAsync(nhanVien.MaNV);

            if (existing == null)
                return false;

            if (nhanVien.LuongCoBan < 0)
                throw new ArgumentException("Luong co ban khong duoc am.");

            existing.HoTen = nhanVien.HoTen.Trim();
            existing.GioiTinh = nhanVien.GioiTinh;
            existing.NgaySinh = nhanVien.NgaySinh;
            existing.SoDienThoai = nhanVien.SoDienThoai;
            existing.Email = nhanVien.Email;
            existing.DiaChi = nhanVien.DiaChi;
            existing.ChucVu = nhanVien.ChucVu.Trim();
            existing.LuongCoBan = nhanVien.LuongCoBan;
            existing.NgayVaoLam = nhanVien.NgayVaoLam;
            existing.TrangThai = nhanVien.TrangThai;

            await _repository.UpdateAsync(existing);
            return true;
        }

        public async Task<bool> ChangeStatusAsync(int id)
        {
            var nhanVien = await _repository.GetByIdAsync(id);

            if (nhanVien == null)
                return false;

            nhanVien.TrangThai = !nhanVien.TrangThai;
            await _repository.UpdateAsync(nhanVien);

            return true;
        }

        public Task<(int Tong, int DangLam, int DaNghi)>
            GetStatisticsAsync()
        {
            return _repository.GetStatisticsAsync();
        }
    }
}
