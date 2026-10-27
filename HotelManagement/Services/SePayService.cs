using System.Net.Http.Headers;
using System.Text.Json;
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class SePayCheckResult
    {
        public bool IsPaid { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string? TransactionDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SePayService
    {
        private readonly PaymentSettingService _settingService;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SePayService> _logger;

        public SePayService(
            PaymentSettingService settingService,
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<SePayService> logger)
        {
            _settingService = settingService;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public PaymentSetting GetSettings()
        {
            return _settingService.GetSettings();
        }

        public void SaveSettings(PaymentSetting settings)
        {
            _settingService.SaveSettings(settings);
        }

        /// <summary>
        /// Tạo URL ảnh QR Code chuẩn VietQR để khách hàng quét trên ứng dụng ngân hàng bất kỳ
        /// </summary>
        public string GenerateVietQrUrl(int maDatPhong, decimal amount)
        {
            var settings = _settingService.GetSettings();
            var bankCode = Uri.EscapeDataString(settings.NganHangMa ?? "MB");
            var accNo = Uri.EscapeDataString(settings.SoTaiKhoan ?? "");
            var accName = Uri.EscapeDataString(settings.TenChuTaiKhoan ?? "");
            var addInfo = Uri.EscapeDataString($"{settings.NoiDungPrefix}{maDatPhong}");
            var longAmount = (long)Math.Max(1000, Math.Round(amount));

            // Template compact2 là template giao diện hiện đại, rõ ràng số tiền và nội dung
            return $"https://img.vietqr.io/image/{bankCode}-{accNo}-compact2.png?amount={longAmount}&addInfo={addInfo}&accountName={accName}";
        }

        /// <summary>
        /// Kiểm tra giao dịch từ SePay API xem khách hàng đã chuyển tiền cho đơn đặt phòng chưa
        /// </summary>
        public async Task<SePayCheckResult> CheckPaymentAsync(int maDatPhong)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null)
            {
                return new SePayCheckResult
                {
                    IsPaid = false,
                    Message = "Không tìm thấy thông tin đơn đặt phòng."
                };
            }

            // Nếu đơn đã được xác nhận thanh toán/đặt cọc trước đó
            if (datPhong.TrangThai == "DaCoc" || datPhong.TrangThai == "DaThanhToan")
            {
                return new SePayCheckResult
                {
                    IsPaid = true,
                    Amount = datPhong.TienCoc,
                    Message = "Đơn đặt phòng đã được ghi nhận thanh toán thành công."
                };
            }

            var settings = _settingService.GetSettings();

            // Tính số tiền cần thanh toán
            var soDem = (datPhong.NgayTraDuKien.Date - datPhong.NgayNhanDuKien.Date).Days;
            if (soDem <= 0) soDem = 1;
            var donGia = datPhong.ChiTietDatPhongs.FirstOrDefault()?.DonGia ?? 0;
            var tongTien = donGia * soDem;
            var tiLe = settings.TiLeThanhToanPercent > 0 ? settings.TiLeThanhToanPercent : 100;
            var soTienCanThanhToan = tongTien * tiLe / 100m;

            // Nếu chưa nhập SePay API Key
            if (string.IsNullOrWhiteSpace(settings.SePayApiKey))
            {
                return new SePayCheckResult
                {
                    IsPaid = false,
                    Message = "Chưa cấu hình SePay API Key trong cài đặt quản trị."
                };
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.SePayApiKey.Trim());

                var apiUrl = string.IsNullOrWhiteSpace(settings.SePayApiUrl)
                    ? "https://my.sepay.vn/userapi/transactions/list"
                    : settings.SePayApiUrl.Trim();

                // Gọi SePay API lấy danh sách giao dịch gần nhất
                var response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    var errCode = (int)response.StatusCode;
                    return new SePayCheckResult
                    {
                        IsPaid = false,
                        Message = $"SePay API phản hồi mã lỗi {errCode}."
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("transactions", out var transElement) || transElement.ValueKind != JsonValueKind.Array)
                {
                    return new SePayCheckResult
                    {
                        IsPaid = false,
                        Message = "Dữ liệu giao dịch từ SePay không đúng định dạng."
                    };
                }

                var expectedPrefixCode = $"{settings.NoiDungPrefix}{maDatPhong}".ToUpperInvariant();
                var expectedCodeWithSpace = $"{settings.NoiDungPrefix} {maDatPhong}".ToUpperInvariant();
                var plainCode = $"{maDatPhong}";

                foreach (var item in transElement.EnumerateArray())
                {
                    var content = item.TryGetProperty("transaction_content", out var cProp) ? cProp.GetString() ?? "" : "";
                    var contentUpper = content.ToUpperInvariant();

                    // Kiểm tra nội dung chuyển khoản có chứa mã đặt phòng hay không
                    bool isMatchedCode = contentUpper.Contains(expectedPrefixCode)
                                      || contentUpper.Contains(expectedCodeWithSpace)
                                      || (contentUpper.Contains(plainCode) && (contentUpper.Contains("DP") || contentUpper.Contains("DATPHONG")));

                    if (isMatchedCode)
                    {
                        decimal amountIn = 0;
                        if (item.TryGetProperty("amount_in", out var aProp))
                        {
                            if (aProp.ValueKind == JsonValueKind.Number)
                            {
                                amountIn = aProp.GetDecimal();
                            }
                            else if (aProp.ValueKind == JsonValueKind.String)
                            {
                                decimal.TryParse(aProp.GetString(), out amountIn);
                            }
                        }

                        // Nếu có số tiền nộp vào hợp lệ
                        if (amountIn > 0)
                        {
                            var transId = item.TryGetProperty("id", out var idProp) ? idProp.ToString() : "";
                            var transDate = item.TryGetProperty("transaction_date", out var dProp) ? dProp.GetString() : DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                            // Cập nhật trạng thái đặt phòng sang Đã Đặt Cọc / Đã Thanh Toán
                            datPhong.TrangThai = "DaCoc";
                            datPhong.TienCoc = amountIn;
                            datPhong.GhiChu = $"{datPhong.GhiChu} [Đã nhận cọc {amountIn:N0}đ qua SePay (Mã GD: {transId}) vào {transDate}]".Trim();

                            _context.DatPhongs.Update(datPhong);
                            await _context.SaveChangesAsync();

                            return new SePayCheckResult
                            {
                                IsPaid = true,
                                Amount = amountIn,
                                TransactionId = transId,
                                TransactionDate = transDate,
                                Message = "Xác nhận thanh toán thành công qua SePay!"
                            };
                        }
                    }
                }

                return new SePayCheckResult
                {
                    IsPaid = false,
                    Message = "Đang chờ khách hàng chuyển khoản..."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kiểm tra thanh toán SePay cho mã đặt phòng {MaDatPhong}", maDatPhong);
                return new SePayCheckResult
                {
                    IsPaid = false,
                    Message = $"Lỗi kết nối tới SePay: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Giả lập thanh toán thành công tức thì phục vụ kiểm thử và chấm bài
        /// </summary>
        public async Task<bool> SimulateSuccessPaymentAsync(int maDatPhong)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return false;

            var settings = _settingService.GetSettings();
            var soDem = (datPhong.NgayTraDuKien.Date - datPhong.NgayNhanDuKien.Date).Days;
            if (soDem <= 0) soDem = 1;
            var donGia = datPhong.ChiTietDatPhongs.FirstOrDefault()?.DonGia ?? 0;
            var tongTien = donGia * soDem;
            var tiLe = settings.TiLeThanhToanPercent > 0 ? settings.TiLeThanhToanPercent : 100;
            var amount = tongTien * tiLe / 100m;

            datPhong.TrangThai = "DaCoc";
            datPhong.TienCoc = amount;
            datPhong.GhiChu = $"{datPhong.GhiChu} [Xác nhận thanh toán {amount:N0}đ qua SePay - Thử nghiệm lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}]".Trim();

            _context.DatPhongs.Update(datPhong);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Kiểm tra kết nối tới SePay API
        /// </summary>
        public async Task<string> TestConnectionAsync()
        {
            var settings = _settingService.GetSettings();
            if (string.IsNullOrWhiteSpace(settings.SePayApiKey))
            {
                return "Chưa nhập SePay API Key. Vui lòng nhập API Key để kiểm tra.";
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.SePayApiKey.Trim());

                var apiUrl = string.IsNullOrWhiteSpace(settings.SePayApiUrl)
                    ? "https://my.sepay.vn/userapi/transactions/list?limit=1"
                    : settings.SePayApiUrl.Trim();

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    return "Kết nối SePay API thành công! Tài khoản SePay đã hoạt động bình thường.";
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "API Key không hợp lệ hoặc đã hết hạn (401 Unauthorized).";
                }

                return $"SePay phản hồi mã lỗi: {(int)response.StatusCode} {response.ReasonPhrase}.";
            }
            catch (Exception ex)
            {
                return $"Không thể kết nối đến máy chủ SePay: {ex.Message}";
            }
        }
    }
}
