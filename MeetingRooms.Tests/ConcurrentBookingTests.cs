using MeetingRooms.Api.Data;
using MeetingRooms.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Tests;

public class ConcurrentBookingTests
{
    // Runs against a real SQL Server: the in-memory provider has no unique
    // indexes, so it could not prove anything about concurrency.
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Server=localhost;Database=MeetingRooms_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    [Fact]
    public async Task ParallelBookingsForSameSlot_ExactlyOneSucceeds()
    {
        await using (var setup = CreateContext())
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.MigrateAsync();
        }

        const int requests = 20;
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // All tasks wait on this gate and are released at the same moment.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, requests).Select(i => Task.Run(async () =>
        {
            await gate.Task;
            await using var db = CreateContext(); // each request gets its own DbContext, like real HTTP requests
            return await new BookingService(db).BookAsync(roomId: 1, date, startHour: 10, userId: $"user-{i}");
        })).ToList();

        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, results.Count(r => r == BookingResult.Success));
        Assert.Equal(requests - 1, results.Count(r => r == BookingResult.SlotTaken));

        await using var check = CreateContext();
        Assert.Equal(1, await check.Bookings.CountAsync(b => b.RoomId == 1 && b.Date == date && b.StartHour == 10));
    }
}