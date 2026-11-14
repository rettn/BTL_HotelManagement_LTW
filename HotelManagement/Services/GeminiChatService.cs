using System.Net.Http.Json;
using System.Text.Json;
using HotelManagement.Configuration;
using HotelManagement.Data;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public sealed class GeminiChatService : IGeminiChatService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ApplicationDbContext _db;
    private readonly IDataProtector _apiKeyProtector;
    private readonly ILogger<GeminiChatService> _logger;

    public GeminiChatService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ApplicationDbContext db,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<GeminiChatService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _db = db;
        _apiKeyProtector = dataProtectionProvider.CreateProtector("HotelManagement.GeminiApiKey.v1");
        _logger = logger;
    }

    public async Task<string> AskAsync(string systemInstruction, string question, CancellationToken cancellationToken = default)
    {
        var settings = await _db.CaiDatHeThongs.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var apiKey = _options.ApiKey;
        if (!string.IsNullOrWhiteSpace(settings?.GeminiApiKeyProtected))
        {
            try
            {
                apiKey = _apiKeyProtector.Unprotect(settings.GeminiApiKeyProtected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể giải mã Gemini API key trong cài đặt.");
                throw new InvalidOperationException("Gemini API key trong cài đặt không hợp lệ.");
            }
        }

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Chưa cấu hình Gemini:ApiKey.");

        var model = string.IsNullOrWhiteSpace(settings?.GeminiModel) ? _options.Model : settings.GeminiModel;
        // Đảm bảo loại bỏ tiền tố "models/" nếu trong DB lỡ lưu thừa
        model = model.Replace("models/", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(model)) model = "gemini-2.5-flash";

        var timeoutSeconds = settings?.GeminiTimeoutSeconds is > 0 and <= 120
            ? settings.GeminiTimeoutSeconds
            : (_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 60);

        // Dùng URL tuyệt đối để tránh lỗi mất /v1beta của BaseAddress
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = question } } } },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = _options.MaxResponseTokens > 0 ? _options.MaxResponseTokens : 1024
            }
        };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(15, timeoutSeconds)));

        // Dùng HttpRequestMessage ép HTTP/1.1 để tránh lỗi SocketException (995) khi đàm phán HTTP/2 trên Windows
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Version = new Version(1, 1),
            Content = JsonContent.Create(payload)
        };

        using var response = await _httpClient.SendAsync(requestMessage, timeoutCts.Token);
        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini request failed with status {StatusCode}: {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Gemini trả về lỗi {(int)response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        var text = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return string.IsNullOrWhiteSpace(text)
            ? "Xin lỗi, AI chưa tạo được câu trả lời. Vui lòng thử lại."
            : text.Trim();
    }
}
