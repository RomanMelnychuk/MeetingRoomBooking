using MeetingRooms.Api.Data;
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

    // SQL Server: 2601 = duplicate key in unique index, 2627 = unique constraint violation.
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);
}