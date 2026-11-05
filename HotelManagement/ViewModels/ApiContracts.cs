using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels;

public sealed record ApiPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record RoomApiDto(int Id, string Number, string Status, int RoomTypeId, string RoomType, decimal PricePerNight, int MaxOccupancy);
public sealed record ItemApiDto(int Id, string Code, string Name, string Type, string? Group, decimal Price, int? Stock, bool Active, string? ImageUrl);
public sealed record BookingRoomApiDto(int DetailId, int RoomId, string RoomNumber, string RoomType, DateTime CheckIn, DateTime CheckOut, decimal PricePerNight, DateTime? ActualCheckIn, DateTime? ActualCheckOut);
public sealed record BookingApiDto(int Id, string Code, string Status, DateTime CreatedAt, DateTime PlannedCheckIn, DateTime PlannedCheckOut, decimal Deposit, int GuestId, string GuestName, string? GuestPhone, IReadOnlyList<BookingRoomApiDto> Rooms);

public sealed class CreateBookingApiRequest
{
    [Range(1, int.MaxValue)] public int GuestId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    [Range(0, 1_000_000_000)] public decimal Deposit { get; set; }
    [MinLength(1)] public List<int> RoomIds { get; set; } = new();
    [StringLength(500)] public string? Note { get; set; }
}
