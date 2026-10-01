namespace MeetingRooms.Api.Models;

/// <summary>Fixed hourly slots, identical for every room: 09:00–18:00.</summary>
public static class TimeSlots
{
    public const int FirstHour = 9;
    public const int LastHour = 17; // last slot starts at 17:00

    public static bool IsValid(int startHour) =>
        startHour >= FirstHour && startHour <= LastHour;
}