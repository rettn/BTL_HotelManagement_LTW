
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

        public async Task<(bool ThanhCong, string ThongBao)> AddAsync(
            Phong phong)
        {
            phong.SoPhong = phong.SoPhong.Trim();

            if (string.IsNullOrWhiteSpace(phong.SoPhong))
            {
                return (false, "Vui long nhap so phong.");
            }

            // Quy uoc: 10x la tang 1, 20x la tang 2...
            if (!int.TryParse(phong.SoPhong, out int so))
            {
                return (false, "So phong phai la chu so.");
            }

            int tang = so / 100;

            if (tang < 1 || tang > 99 || so % 100 == 0)
            {
                return (false,
                    "So phong khong hop le. Vi du: 101, 201, 301.");
            }

            if (await _repository.ExistsSoPhongAsync(phong.SoPhong))
            {
                return (false, "So phong nay da ton tai.");
            }

            var danhSachLoaiPhong =
                await _repository.GetLoaiPhongsAsync();

            if (!danhSachLoaiPhong.Any(
                lp => lp.MaLoaiPhong == phong.MaLoaiPhong))
            {
                return (false, "Loai phong khong ton tai.");
            }

            phong.TrangThai = "Trong";

            await _repository.AddAsync(phong);

            return (true, "Them phong thanh cong.");
        }
    }
}
