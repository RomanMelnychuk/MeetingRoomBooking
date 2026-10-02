using MeetingRooms.Api.Data;
using MeetingRooms.Api.Hubs;
using MeetingRooms.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<BookingService>();
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// Locally SignalR runs in-process; in Azure it goes through Azure SignalR Service
// once the connection string "Azure:SignalR:ConnectionString" is configured.
var signalR = builder.Services.AddSignalR();
if (!string.IsNullOrEmpty(builder.Configuration["Azure:SignalR:ConnectionString"]))
    signalR.AddAzureSignalR();

var app = builder.Build();
await DbSeeder.SeedAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapGroup("/api/auth").MapIdentityApi<IdentityUser>();

app.MapControllers();

app.MapHub<ScheduleHub>("/hubs/schedule");

app.Run();
