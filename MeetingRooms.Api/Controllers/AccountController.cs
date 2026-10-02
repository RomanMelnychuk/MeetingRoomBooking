using MeetingRooms.Api.Data;
using MeetingRooms.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRooms.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AccountController(SignInManager<IdentityUser> signInManager) : ControllerBase
{
    [HttpGet("me")]
    public MeDto Me() => new(User.Identity!.Name!, User.IsInRole(DbSeeder.AdminRole));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }
}