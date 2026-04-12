using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeShare.API.DTOs;
using WeShare.API.Services;

namespace WeShare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 🔒 SECURED!
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        try
        {
            var profile = await _userService.GetProfileAsync(id);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileRequest request)
    {
        try
        {
            // EXTRA SECURITY: Bawal mong i-update ang profile ng iba!
            if (id != GetUserIdFromToken())
                return Forbid();

            var updatedProfile = await _userService.UpdateProfileAsync(id, request);
            return Ok(updatedProfile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }
}