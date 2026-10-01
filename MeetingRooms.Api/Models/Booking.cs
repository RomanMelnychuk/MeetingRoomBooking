namespace MeetingRooms.Api.Models;

public class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public DateOnly Date { get; set; }
    public int StartHour { get; set; }               // 9 = slot 09:00–10:00
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}