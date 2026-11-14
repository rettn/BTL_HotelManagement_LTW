namespace HotelManagement.Services;

public interface IGeminiChatService
{
    Task<string> AskAsync(string systemInstruction, string question, CancellationToken cancellationToken = default);
}
