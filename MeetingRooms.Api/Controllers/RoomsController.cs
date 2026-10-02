using System.Security.Claims;
using MeetingRooms.Api.Data;
using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Models;
using MeetingRooms.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize] // any logged-in user can view rooms and schedules
public class RoomsController(AppDbContext db, BookingService bookings) : ControllerBase
{
    [HttpGet]
    public async Task<List<RoomDto>> GetAll() =>
        await db.Rooms
            .OrderBy(r => r.Name)
            .Select(r => new RoomDto(r.Id, r.Name, r.Capacity))
            .ToListAsync();

    [HttpGet("{id:int}/schedule")]
    public async Task<ActionResult<List<SlotDto>>> GetSchedule(int id, [FromQuery] DateOnly date)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var slots = await bookings.GetScheduleAsync(id, date, userId);
        return slots is null ? NotFound() : slots;
    }

    [HttpPost, Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<ActionResult<RoomDto>> Create(SaveRoomRequest request)
    {
        var room = new Room { Name = request.Name, Capacity = request.Capacity };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return Created($"/api/rooms/{room.Id}", new RoomDto(room.Id, room.Name, room.Capacity));
    }

    [HttpPut("{id:int}"), Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Update(int id, SaveRoomRequest request)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room is null) return NotFound();

        room.Name = request.Name;
        room.Capacity = request.Capacity;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room is null) return NotFound();

        db.Rooms.Remove(room); // the room's bookings are removed by cascade delete
        await db.SaveChangesAsync();
        return NoContent();
    }
}