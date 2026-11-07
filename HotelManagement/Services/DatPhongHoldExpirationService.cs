using HotelManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    /// <summary>
    /// Tự động giải phóng các phiếu đặt phòng online chưa thanh toán
    /// sau khi hết thời gian giữ phòng.
    /// </summary>
    public sealed class DatPhongHoldExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatPhongHoldExpirationService> _logger;

        public DatPhongHoldExpirationService(
            IServiceScopeFactory scopeFactory,
            ILogger<DatPhongHoldExpirationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chạy ngay lần đầu, sau đó kiểm tra định kỳ mỗi phút.
            await ReleaseExpiredBookingsAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ReleaseExpiredBookingsAsync(stoppingToken);
            }
        }

        private async Task ReleaseExpiredBookingsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var datPhongService = scope.ServiceProvider.GetRequiredService<DatPhongService>();

                var settings = await context.CaiDatHeThongs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);
                var holdMinutes = Math.Clamp(settings?.ThoiHanGiuPhong ?? 15, 5, 120);
                var deadline = DateTime.Now.AddMinutes(-holdMinutes);

                var expiredIds = await context.DatPhongs
                    .AsNoTracking()
                    .Where(d => d.TrangThai == "ChoXacNhan"
                        && d.NgayDat <= deadline
                        && d.GhiChu != null
                        && d.GhiChu.Contains("[Khách tự đặt trực tuyến]"))
                    .Select(d => d.MaDatPhong)
                    .ToListAsync(cancellationToken);

                foreach (var id in expiredIds)
                {
                    var result = await datPhongService.CancelAsync(id);
                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Đã tự động hủy phiếu đặt phòng online {MaDatPhong} do hết thời gian giữ phòng.",
                            id);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Ứng dụng đang dừng, không cần ghi lỗi.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tự động giải phóng phiếu đặt phòng hết hạn.");
            }
        }
    }
}
