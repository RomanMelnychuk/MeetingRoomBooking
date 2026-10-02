using System.ComponentModel.DataAnnotations;

namespace MeetingRooms.Api.Dtos;

public record RoomDto(int Id, string Name, int Capacity);

public record SaveRoomRequest(
    [Required, StringLength(100)] string Name,
    [Range(1, 100)] int Capacity);

public record SlotDto(int StartHour, bool IsBooked, bool IsMine);

public record CreateBookingRequest(int RoomId, DateOnly Date, int StartHour);

public record BookingDto(int Id, string RoomName, DateOnly Date, int StartHour, string UserEmail, DateTime CreatedAtUtc);

public record MeDto(string Email, bool IsAdmin);