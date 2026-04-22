using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeShare.API.DTOs;
using WeShare.API.Services;

namespace WeShare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 🔒 Secured!
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Gets all users (used for search and suggestions).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Gets a specific user profile.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        var profile = await _userService.GetProfileAsync(id);
        if (profile == null) return NotFound(new { message = "User not found." });
        return Ok(profile);
    }

    /// <summary>
    /// Updates a user profile, accepting multipart/form-data for image uploads.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromForm] UpdateProfileRequest request)
    {
        try
        {
            // 🚨 AUTHORIZATION GATE: Prevent users from editing other people's profiles
            var currentUserIdStr = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (currentUserIdStr == null || Guid.Parse(currentUserIdStr) != id)
            {
                return StatusCode(403, new { message = "You are not authorized to update this profile." });
            }

            var updatedProfile = await _userService.UpdateProfileAsync(id, request);
            return Ok(updatedProfile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}