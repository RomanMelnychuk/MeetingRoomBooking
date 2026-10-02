using MeetingRooms.Api.Data;
using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Api.Services;

public enum BookingResult { Success, SlotTaken, InvalidSlot, RoomNotFound }

public class BookingService(AppDbContext db)
{
    /// <summary>
    /// Books a slot. Double booking is prevented by the unique index on
    /// (RoomId, Date, StartHour): we insert directly and let the database
    /// reject the loser, instead of a racy "check if free, then insert".
    /// </summary>
    public async Task<BookingResult> BookAsync(int roomId, DateOnly date, int startHour, string userId)
    {
        if (!TimeSlots.IsValid(startHour) || date < DateOnly.FromDateTime(DateTime.UtcNow))
            return BookingResult.InvalidSlot;

        if (!await db.Rooms.AnyAsync(r => r.Id == roomId))
            return BookingResult.RoomNotFound;

        db.Bookings.Add(new Booking
        {
            RoomId = roomId,
            Date = date,
            StartHour = startHour,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync();
            return BookingResult.Success;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return BookingResult.SlotTaken;
        }
    }

    /// <summary>Returns every fixed slot of the day with its status, or null if the room doesn't exist.</summary>
    public async Task<List<SlotDto>?> GetScheduleAsync(int roomId, DateOnly date, string userId)
    {
        if (!await db.Rooms.AnyAsync(r => r.Id == roomId))
            return null;

        var booked = await db.Bookings
            .Where(b => b.RoomId == roomId && b.Date == date)
            .Select(b => new { b.StartHour, b.UserId })
            .ToListAsync();

        return Enumerable.Range(TimeSlots.FirstHour, TimeSlots.LastHour - TimeSlots.FirstHour + 1)
            .Select(hour =>
            {
                var booking = booked.FirstOrDefault(b => b.StartHour == hour);
                return new SlotDto(hour, booking is not null, booking?.UserId == userId);
            })
            .ToList();
    }

    // SQL Server: 2601 = duplicate key in unique index, 2627 = unique constraint violation.
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);
}