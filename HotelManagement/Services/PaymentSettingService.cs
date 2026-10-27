using System.Text.Json;
using HotelManagement.Models;

namespace HotelManagement.Services
{
    public class PaymentSettingService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PaymentSettingService> _logger;
        private readonly string _filePath;
        private static readonly object _lock = new();
        private PaymentSetting? _cachedSettings;

        public PaymentSettingService(IWebHostEnvironment env, ILogger<PaymentSettingService> logger)
        {
            _env = env;
            _logger = logger;
            _filePath = Path.Combine(_env.ContentRootPath, "payment_settings.json");
        }

        public PaymentSetting GetSettings()
        {
            lock (_lock)
            {
                if (_cachedSettings != null) return _cachedSettings;

                try
                {
                    if (File.Exists(_filePath))
                    {
                        var json = File.ReadAllText(_filePath);
                        var settings = JsonSerializer.Deserialize<PaymentSetting>(json);
                        if (settings != null)
                        {
                            _cachedSettings = settings;
                            return _cachedSettings;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi đọc file payment_settings.json");
                }

                _cachedSettings = new PaymentSetting();
                SaveSettings(_cachedSettings);
                return _cachedSettings;
            }
        }

        public void SaveSettings(PaymentSetting settings)
        {
            lock (_lock)
            {
                try
                {
                    // Tự động gán tên ngân hàng dựa theo mã
                    var bank = PaymentSetting.DanhSachNganHang.FirstOrDefault(b => b.Ma.Equals(settings.NganHangMa, StringComparison.OrdinalIgnoreCase));
                    if (bank != null)
                    {
                        settings.TenNganHang = bank.Ten;
                    }

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(settings, options);
                    File.WriteAllText(_filePath, json);
                    _cachedSettings = settings;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi ghi file payment_settings.json");
                }
            }
        }
    }
}
