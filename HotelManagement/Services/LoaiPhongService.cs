
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

        public Task<List<LoaiPhong>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public Task<LoaiPhong?> GetByIdAsync(int id)
        {
            if (id <= 0)
                return Task.FromResult<LoaiPhong?>(null);

            return _repository.GetByIdAsync(id);
        }

        public Task<List<LoaiPhong>> SearchAsync(string? keyword)
        {
            return _repository.SearchAsync(keyword);
        }

        public async Task<(bool ThanhCong, string ThongBao)> AddAsync(
            LoaiPhong loaiPhong)
        {
            string? loi = Validate(loaiPhong);

            if (loi != null)
                return (false, loi);

            loaiPhong.TenLoaiPhong =
                loaiPhong.TenLoaiPhong.Trim();

            loaiPhong.MoTa =
                string.IsNullOrWhiteSpace(loaiPhong.MoTa)
                    ? null
                    : loaiPhong.MoTa.Trim();

            if (await _repository.ExistsNameAsync(
                loaiPhong.TenLoaiPhong))
            {
                return (false, "Tên loại phòng đã tồn tại.");
            }

            await _repository.AddAsync(loaiPhong);

            return (true, "Thêm loại phòng thành công.");
        }

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

            string tenMoi = loaiPhong.TenLoaiPhong.Trim();

            if (await _repository.ExistsNameAsync(
                tenMoi, loaiPhong.MaLoaiPhong))
            {
                return (false, "Tên loại phòng đã tồn tại.");
            }

            hienTai.TenLoaiPhong = tenMoi;
            hienTai.MoTa = string.IsNullOrWhiteSpace(loaiPhong.MoTa)
                ? null
                : loaiPhong.MoTa.Trim();
            hienTai.SoNguoiToiDa = loaiPhong.SoNguoiToiDa;
            hienTai.GiaTheoDem = loaiPhong.GiaTheoDem;

            await _repository.UpdateAsync(hienTai);

            return (true, "Cập nhật loại phòng thành công.");
        }

        public async Task<(bool ThanhCong, string ThongBao)> DeleteAsync(
            int id)
        {
            if (id <= 0)
                return (false, "Mã loại phòng không hợp lệ.");

            var loaiPhong = await _repository.GetByIdAsync(id);

            if (loaiPhong == null)
                return (false, "Không tìm thấy loại phòng.");

            if (await _repository.IsBeingUsedAsync(id))
            {
                return (false,
                    "Không thể xóa loại phòng đang được phòng sử dụng.");
            }

            bool thanhCong = await _repository.DeleteAsync(id);

            return thanhCong
                ? (true, "Xóa loại phòng thành công.")
                : (false, "Không thể xóa loại phòng.");
        }

        private static string? Validate(LoaiPhong loaiPhong)
        {
            if (string.IsNullOrWhiteSpace(loaiPhong.TenLoaiPhong))
                return "Vui lòng nhập tên loại phòng.";

            if (loaiPhong.TenLoaiPhong.Trim().Length > 100)
                return "Tên loại phòng tối đa 100 ký tự.";

            if (loaiPhong.MoTa?.Length > 500)
                return "Mô tả tối đa 500 ký tự.";

            if (loaiPhong.SoNguoiToiDa < 1 ||
                loaiPhong.SoNguoiToiDa > 100)
                return "Số người tối đa phải từ 1 đến 100.";

            if (loaiPhong.GiaTheoDem < 0)
                return "Giá theo đêm không được âm.";

            return null;
        }
    }
}
