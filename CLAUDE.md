# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

Meeting room booking system with concurrency control and real-time updates.
- Backend: ASP.NET Core (.NET 10), EF Core, SQL Server / Azure SQL, ASP.NET Core Identity, SignalR (Azure SignalR Service in production).
- Frontend: React + Vite in `meeting-rooms-web/`, built into `MeetingRooms.Api/wwwroot`, so one Azure Web App serves both.
- Tests: xUnit in `MeetingRooms.Tests/`, running against a real SQL Server.

## Structure

```
MeetingRooms.Api/
  Controllers/   REST endpoints (rooms, bookings, account)
  Services/      BookingService: booking logic and conflict handling
  Hubs/          ScheduleHub: notification-only SignalR hub
  Data/          AppDbContext, DbSeeder (migrations + admin seeding)
  Models/        Room, Booking, TimeSlots
  Dtos/          Request/response records
MeetingRooms.Tests/   Concurrency test
meeting-rooms-web/    React frontend
```

## Commands

```bash
dotnet build
dotnet test                                                  # needs local SQL Server (see README)
dotnet run --project MeetingRooms.Api --launch-profile https # backend on https://localhost:7185
cd meeting-rooms-web && npm run dev                          # frontend on http://localhost:5173
cd meeting-rooms-web && npm run build                        # outputs to MeetingRooms.Api/wwwroot
dotnet ef migrations add <Name> --project MeetingRooms.Api
```

## Rules that must not be broken

1. **Double booking is prevented by the unique index on `(RoomId, Date, StartHour)`.**
   Never add a "check if free, then insert" step as the protection. Insert directly and
   map the unique-violation `DbUpdateException` (SQL errors 2601/2627) to `409 Conflict`.
2. **A booking race must never produce a 500.** Every `BookingResult` maps to a clear status code.
3. **SignalR only signals.** Hub events (`SlotBooked`, `RoomsChanged`) carry identifiers, never
   schedule data. Clients re-read data through the REST API. Do not move data endpoints into the hub.
4. **Every user action in the UI gets visible feedback**: disabled button while the request runs,
   then a toast with the result (success or the server's error message).
5. **No template leftovers**: remove scaffolded sample files (WeatherForecast, Vite logos, default CSS).
6. Do not expose EF entities from controllers; use records from `Dtos/`.
7. Keep the concurrency test passing: `ParallelBookingsForSameSlot_ExactlyOneSucceeds`.

## Conventions

- Errors are returned as `ProblemDetails` with a human-readable `title`; the frontend shows it as is.
- Admin-only endpoints use `[Authorize(Roles = DbSeeder.AdminRole)]`.
- Secrets (connection strings, admin password) come from configuration / Azure App Settings, never from code.
- Commits are atomic: one logical change per commit, message says what changed and why.