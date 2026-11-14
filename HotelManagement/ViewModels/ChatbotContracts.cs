namespace HotelManagement.ViewModels;

public sealed class ChatbotRequest
{
    public string Message { get; set; } = string.Empty;
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
}

public sealed class ChatbotResponse
{
    public string Reply { get; init; } = string.Empty;
    public string Audience { get; init; } = "customer";
}
