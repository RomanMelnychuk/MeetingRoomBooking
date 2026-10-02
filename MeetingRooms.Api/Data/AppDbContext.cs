using MeetingRooms.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // creates Identity tables (users, roles)

        modelBuilder.Entity<Room>(room =>
        {
            room.Property(r => r.Name).HasMaxLength(100).IsRequired();
            room.HasData(
                new Room { Id = 1, Name = "Small Room", Capacity = 4 },
                new Room { Id = 2, Name = "Big Room", Capacity = 12 });
        });

        // Concurrency guarantee: the database itself refuses a second booking
        // for the same room, date and hour, even if two requests arrive at once.
        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.RoomId, b.Date, b.StartHour })
            .IsUnique();
    }
}