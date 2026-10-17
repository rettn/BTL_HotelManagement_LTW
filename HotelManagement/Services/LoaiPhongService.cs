
using HotelManagement.Models;
using HotelManagement.Repositories;

namespace HotelManagement.Services
{
    public class LoaiPhongService
    {
        private readonly LoaiPhongRepository _repository;

        public LoaiPhongService(LoaiPhongRepository repository)
        {
            _repository = repository;
        }

        // Lay danh sach loai phong
        public async Task<List<LoaiPhong>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        // Tim theo ma
        public async Task<LoaiPhong?> GetByIdAsync(int id)
        {
            if (id <= 0)
                return null;

            return await _repository.GetByIdAsync(id);
        }

        // Them loai phong
        public async Task<(bool ThanhCong, string ThongBao)> AddAsync(
            LoaiPhong loaiPhong)
        {
            string? loi = Validate(loaiPhong);

            if (loi != null)
                return (false, loi);

            var danhSach = await _repository.GetAllAsync();

            if (danhSach.Any(lp =>
                lp.TenLoaiPhong.Trim().Equals(
                    loaiPhong.TenLoaiPhong.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "Tên loại phòng đã tồn tại.");
            }

            loaiPhong.TenLoaiPhong = loaiPhong.TenLoaiPhong.Trim();
            loaiPhong.MoTa = loaiPhong.MoTa?.Trim();

            await _repository.AddAsync(loaiPhong);

            return (true, "Thêm loại phòng thành công.");
        }

        // Cap nhat loai phong
        public async Task<(bool ThanhCong, string ThongBao)> UpdateAsync(
            LoaiPhong loaiPhong)
        {
            if (loaiPhong.MaLoaiPhong <= 0)
                return (false, "Mã loại phòng không hợp lệ.");

            string? loi = Validate(loaiPhong);

            if (loi != null)
                return (false, loi);

            var hienTai = await _repository.GetByIdAsync(
                loaiPhong.MaLoaiPhong);

            if (hienTai == null)
                return (false, "Không tìm thấy loại phòng.");

            var danhSach = await _repository.GetAllAsync();

            if (danhSach.Any(lp =>
                lp.MaLoaiPhong != loaiPhong.MaLoaiPhong &&
                lp.TenLoaiPhong.Trim().Equals(
                    loaiPhong.TenLoaiPhong.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "Tên loại phòng đã tồn tại.");
            }

            hienTai.TenLoaiPhong = loaiPhong.TenLoaiPhong.Trim();
            hienTai.MoTa = loaiPhong.MoTa?.Trim();
            hienTai.SoNguoiToiDa = loaiPhong.SoNguoiToiDa;
            hienTai.GiaTheoDem = loaiPhong.GiaTheoDem;

            await _repository.UpdateAsync(hienTai);

            return (true, "Cập nhật loại phòng thành công.");
        }

        // Xoa loai phong
        public async Task<(bool ThanhCong, string ThongBao)> DeleteAsync(
            int id)
        {
            if (id <= 0)
                return (false, "Mã loại phòng không hợp lệ.");

            var loaiPhong = await _repository.GetByIdAsync(id);

            if (loaiPhong == null)
                return (false, "Không tìm thấy loại phòng.");

            bool thanhCong = await _repository.DeleteAsync(id);

            if (!thanhCong)
            {
                return (false,
                    "Không thể xóa loại phòng đang được phòng sử dụng.");
            }

            return (true, "Xóa loại phòng thành công.");
        }

        // Tim kiem
        public async Task<List<LoaiPhong>> SearchAsync(string? keyword)
        {
            return await _repository.SearchAsync(keyword?.Trim());
        }

        // Kiem tra du lieu
        private static string? Validate(LoaiPhong loaiPhong)
        {
            if (string.IsNullOrWhiteSpace(loaiPhong.TenLoaiPhong))
                return "Tên loại phòng không được để trống.";

            if (loaiPhong.TenLoaiPhong.Trim().Length > 100)
                return "Tên loại phòng không được vượt quá 100 ký tự.";

            if (loaiPhong.SoNguoiToiDa < 1 ||
                loaiPhong.SoNguoiToiDa > 100)
                return "Số người tối đa phải từ 1 đến 100.";

            if (loaiPhong.GiaTheoDem < 0)
                return "Giá theo đêm không được âm.";

            return null;
        }
    }
}
