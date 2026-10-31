
using HotelManagement.Models;
using HotelManagement.Repositories;

namespace HotelManagement.Services
{
    public class BangLuongService
    {
        private readonly BangLuongRepository _bangLuongRepository;
        private readonly ThuongRepository _thuongRepository;
        private readonly PhuCapRepository _phuCapRepository;
        private readonly KhauTruRepository _khauTruRepository;
        private readonly NhanVienRepository _nhanVienRepository;

        public BangLuongService(
            BangLuongRepository bangLuongRepository,
            ThuongRepository thuongRepository,
            PhuCapRepository phuCapRepository,
            KhauTruRepository khauTruRepository,
            NhanVienRepository nhanVienRepository)
        {
            _bangLuongRepository = bangLuongRepository;
            _thuongRepository = thuongRepository;
            _phuCapRepository = phuCapRepository;
            _khauTruRepository = khauTruRepository;
            _nhanVienRepository = nhanVienRepository;
        }

        public Task<List<NhanVien>> GetActiveEmployeesAsync() => _nhanVienRepository.GetActiveEmployeesAsync();

        // Lay bang luong theo thang va nam
        public Task<List<BangLuong>> GetByMonthAsync(int thang, int nam)
        {
            ValidateMonthYear(thang, nam);
            return _bangLuongRepository.GetByMonthAsync(thang, nam);
        }

        // Lay chi tiet mot bang luong
        public Task<BangLuong?> GetByIdAsync(int id)
        {
            return _bangLuongRepository.GetByIdAsync(id);
        }

        // Lay thong ke luong trong thang
        public Task<(int TongNhanVien, decimal TongLuong)>
            GetStatisticsAsync(int thang, int nam)
        {
            ValidateMonthYear(thang, nam);
            return _bangLuongRepository.GetStatisticsAsync(thang, nam);
        }

        // Tinh luong cho toan bo nhan vien dang lam viec
        public async Task<(bool Success, string Message)> CalculateAsync(
            int thang,
            int nam,
            int ngayCongQuyDinh,
            Dictionary<int, decimal> ngayCongThucTe)
        {
            ValidateMonthYear(thang, nam);

            if (ngayCongQuyDinh <= 0 || ngayCongQuyDinh > 31)
                return (false, "Ngay cong quy dinh phai tu 1 den 31.");

            var nhanViens =
                await _bangLuongRepository.GetActiveEmployeesAsync();

            if (nhanViens.Count == 0)
                return (false, "Khong co nhan vien dang lam viec.");

            var dsThuong =
                await _thuongRepository.GetByMonthAsync(thang, nam);

            var dsPhuCap =
                await _phuCapRepository.GetByMonthAsync(thang, nam);

            var dsKhauTru =
                await _khauTruRepository.GetByMonthAsync(thang, nam);

            foreach (var nhanVien in nhanViens)
            {
                decimal ngayCong = ngayCongThucTe.TryGetValue(
                    nhanVien.MaNV, out var cong)
                    ? cong
                    : 0;

                if (ngayCong < 0 || ngayCong > ngayCongQuyDinh)
                {
                    return (false,
                        $"Ngay cong cua nhan vien {nhanVien.HoTen} khong hop le.");
                }

                var bangLuong =
                    await _bangLuongRepository.GetByEmployeeMonthAsync(
                        nhanVien.MaNV, thang, nam);

                // Khong tinh lai bang luong da thanh toan
                if (bangLuong != null &&
                    bangLuong.TrangThai == "DaThanhToan")
                {
                    continue;
                }

                decimal tongThuong = dsThuong
                    .Where(t => t.MaNV == nhanVien.MaNV)
                    .Sum(t => t.SoTien);

                decimal tongPhuCap = dsPhuCap
                    .Where(p => p.MaNV == nhanVien.MaNV)
                    .Sum(p => p.SoTien);

                decimal tongKhauTru = dsKhauTru
                    .Where(k => k.MaNV == nhanVien.MaNV)
                    .Sum(k => k.SoTien);

                decimal luongTheoCong = Math.Round(
                    nhanVien.LuongCoBan / ngayCongQuyDinh * ngayCong,
                    0,
                    MidpointRounding.AwayFromZero);

                decimal thucLinh =
                    luongTheoCong +
                    tongThuong +
                    tongPhuCap -
                    tongKhauTru;

                if (thucLinh < 0)
                    return (false,
                        $"Luong thuc linh cua {nhanVien.HoTen} dang am. Vui long kiem tra cac khoan khau tru.");

                if (bangLuong == null)
                {
                    bangLuong = new BangLuong
                    {
                        MaNV = nhanVien.MaNV,
                        Thang = thang,
                        Nam = nam,
                        TrangThai = "ChuaThanhToan",
                        NgayTao = DateTime.Now
                    };

                    bangLuong.LuongCoBan = nhanVien.LuongCoBan;
                    bangLuong.NgayCongQuyDinh = ngayCongQuyDinh;
                    bangLuong.NgayCongThucTe = ngayCong;
                    bangLuong.LuongTheoCong = luongTheoCong;
                    bangLuong.TongThuong = tongThuong;
                    bangLuong.TongPhuCap = tongPhuCap;
                    bangLuong.TongKhauTru = tongKhauTru;
                    bangLuong.ThucLinh = thucLinh;

                    await _bangLuongRepository.AddAsync(bangLuong);
                }
                else
                {
                    bangLuong.LuongCoBan = nhanVien.LuongCoBan;
                    bangLuong.NgayCongQuyDinh = ngayCongQuyDinh;
                    bangLuong.NgayCongThucTe = ngayCong;
                    bangLuong.LuongTheoCong = luongTheoCong;
                    bangLuong.TongThuong = tongThuong;
                    bangLuong.TongPhuCap = tongPhuCap;
                    bangLuong.TongKhauTru = tongKhauTru;
                    bangLuong.ThucLinh = thucLinh;

                    await _bangLuongRepository.UpdateAsync(bangLuong);
                }
            }

            return (true, "Tinh luong thang thanh cong.");
        }

        private static void ValidateMonthYear(int thang, int nam)
        {
            if (thang < 1 || thang > 12)
                throw new ArgumentException("Thang khong hop le.");

            if (nam < 2000 || nam > 2100)
                throw new ArgumentException("Nam khong hop le.");
        }
    }
}
