using System.Security.Claims;
using MeetingRooms.Api.Data;
using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Hubs;
using MeetingRooms.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController(AppDbContext db, BookingService bookings, IHubContext<ScheduleHub> hub)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await bookings.BookAsync(request.RoomId, request.Date, request.StartHour, userId);

        if (result == BookingResult.Success)
        {
            // Signal only: viewers of this room re-read the schedule through REST.
            await hub.Clients.Group(ScheduleHub.RoomGroup(request.RoomId))
                .SendAsync("SlotBooked", new SlotBookedEvent(request.RoomId, request.Date, request.StartHour));
        }

        // Every outcome maps to a clear status code: the loser of a race gets 409, never 500.
        return result switch
        {
            BookingResult.Success => StatusCode(StatusCodes.Status201Created),
            BookingResult.SlotTaken => Conflict(new ProblemDetails { Title = "This slot has just been booked by someone else." }),
            BookingResult.RoomNotFound => NotFound(new ProblemDetails { Title = "Room not found." }),
            _ => BadRequest(new ProblemDetails { Title = "Invalid slot or a date in the past." })
        };
    }

    [HttpGet, Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<List<BookingDto>> GetAll() =>
        await (from b in db.Bookings
               join u in db.Users on b.UserId equals u.Id
               orderby b.Date, b.StartHour
               select new BookingDto(b.Id, b.Room.Name, b.Date, b.StartHour, u.Email!, b.CreatedAtUtc))
              .ToListAsync();
}