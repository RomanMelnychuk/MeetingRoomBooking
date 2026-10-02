using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MeetingRooms.Api.Hubs;

/// <summary>
/// Notification-only hub. It never sends schedule data: it tells viewers
/// that something changed, and they re-read the schedule through the REST API.
/// </summary>
[Authorize]
public class ScheduleHub : Hub
{
    public static string RoomGroup(int roomId) => $"room-{roomId}";

    public Task JoinRoom(int roomId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(roomId));

    public Task LeaveRoom(int roomId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(roomId));
}