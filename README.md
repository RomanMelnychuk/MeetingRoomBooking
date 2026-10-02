# Meeting Room Booking System

Booking system for meeting rooms with concurrency control and real-time updates.
Two users trying to book the same slot at the same moment: exactly one succeeds,
the other gets a clear `409 Conflict`. Everyone viewing a room's schedule sees changes instantly.

**Live demo:** https://meetingrooms-roman-d5bva0epgkhfh0eu.polandcentral-01.azurewebsites.net
Admin credentials are sent together with the submission. Regular users can register on the login page.
The first request after idle time can take up to a minute (free tier cold start).

## Tech stack

- **Backend:** ASP.NET Core (.NET 10), EF Core, ASP.NET Core Identity (cookie auth, roles), SignalR
- **Frontend:** React + Vite (built into the API's `wwwroot`, served by the same app)
- **Database:** SQL Server locally, Azure SQL Database in production
- **Real-time:** Azure SignalR Service
- **Hosting:** Azure Web App
- **Tests:** xUnit against a real SQL Server

## Features

- **User:** view rooms and their schedules, book free slots.
- **Admin:** additionally create, edit and delete rooms, and see all bookings across users.
- Every slot is one hour, 09:00–18:00, the same for all rooms.

## Concurrency control (design decision)

**Mechanism:** a unique index on `Bookings (RoomId, Date, StartHour)`.

`BookingService` does **not** check whether a slot is free before inserting.
It inserts directly and lets the database decide. When two requests race,
SQL Server accepts one insert and rejects the other with a unique-key violation
(error 2601/2627). The service catches exactly that error and returns `SlotTaken`,
which the controller maps to `409 Conflict` with a readable message. Any other
database error is not swallowed.

Why this approach:

| Approach | Why not chosen |
|---|---|
| "Check if free, then insert" | Race window between the two steps: both requests see "free" |
| `Serializable` transaction | Range locks block unrelated requests; deadlocks surface as errors unless retried |
| Application lock (`lock`, `SemaphoreSlim`) | Works inside one process only; breaks with several app instances |
| Optimistic concurrency (rowversion) | Needs a pre-existing row per slot; slots here are not stored as rows |

A booking is "room + date + hour", so a conflict is literally "the same row twice".
A unique constraint expresses that directly, holds across any number of app instances,
and never blocks other requests.

**Automated test:** `MeetingRooms.Tests/ConcurrentBookingTests.cs` releases 20 booking
attempts for the same slot at the same moment and asserts that exactly one succeeds,
19 get `SlotTaken`, and exactly one row exists in the database.

## Real-time updates

- `ScheduleHub` is **notification-only**. It never sends schedule data.
- Each room is a SignalR group. A viewer joins the group of the room on screen.
- After a successful booking, the API sends `SlotBooked` to that room's group;
  clients then re-read the schedule through the REST API. Room changes send `RoomsChanged`.
- REST stays the single source of truth; SignalR only says *when* to refresh.
- In Azure, browser connections go through Azure SignalR Service
  (enabled when `Azure:SignalR:ConnectionString` is configured).

## Running locally

**Prerequisites:** .NET 10 SDK, Node.js 20+, SQL Server (or LocalDB).

1. Set your SQL Server in `MeetingRooms.Api/appsettings.json` → `ConnectionStrings:Default`.
2. Backend (applies migrations and seeds the admin on start):
```bash
   dotnet run --project MeetingRooms.Api --launch-profile https
```
3. Frontend (in a second terminal):
```bash
   cd meeting-rooms-web
   npm install
   npm run dev
```
4. Open http://localhost:5173. Dev admin: `admin@test.com` / `Admin123!`
   (from `appsettings.Development.json`).

To try real-time updates, open a second browser window in incognito mode,
register another user, and book a slot in one window.

## Running the tests

The concurrency test needs a real SQL Server: the EF in-memory provider has no unique
indexes and would prove nothing. It creates and drops its own `MeetingRooms_Tests` database.

```bash
dotnet test
```

Default connection: `Server=localhost;Trusted_Connection=True`. To use another server:

```powershell
$env:TEST_DB_CONNECTION="Server=...;Database=MeetingRooms_Tests;..."
dotnet test
```

## Deployment (Azure)

Resources (resource group `meetingrooms-rg`): Azure Web App (Windows, .NET 10),
Azure SQL Database (serverless, free offer), Azure SignalR Service (Free tier, Default mode).

Web App settings:

| Setting | Purpose |
|---|---|
| `ConnectionStrings__Default` | Azure SQL connection string |
| `Azure__SignalR__ConnectionString` | Azure SignalR Service connection string |
| `Admin__Email`, `Admin__Password` | Admin account created on startup |

Build and deploy:

```powershell
cd meeting-rooms-web; npm run build; cd ..
dotnet publish MeetingRooms.Api -c Release -o publish
Compress-Archive -Path publish\* -DestinationPath publish.zip -Force
az webapp deploy --resource-group meetingrooms-rg --name meetingrooms-roman --src-path publish.zip --type zip
```

Migrations run automatically on startup.

## Design decisions and known limitations

- **Fixed slots in code:** hourly 09:00–18:00 for every room (`TimeSlots`). Per-room slot
  sets would need a `TimeSlot` table; not needed for this task.
- **No time zones:** slot hours are wall-clock hours; today's already-passed hours are
  still bookable (dates in the past are rejected).
- **Booking cancellation** is not implemented (not required by the task).
- **Free tiers:** the first request after idle time can be slow (Web App cold start,
  SQL serverless auto-resume). Transient SQL errors are retried (`EnableRetryOnFailure`).

## Development process

Developed with active use of Claude as a pair programmer: architecture discussion,
code generation and debugging. Every change was run and tested locally before committing.
Project rules for Claude are in [CLAUDE.md](CLAUDE.md). Commits are atomic,
each describing what changed and why.