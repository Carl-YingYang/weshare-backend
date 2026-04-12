using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeShare.API.DTOs;
using WeShare.API.Services;

namespace WeShare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 🔒 SECURED!
public class FriendsController : ControllerBase
{
    private readonly IFriendshipService _friendService;

    public FriendsController(IFriendshipService friendService)
    {
        _friendService = friendService;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }

    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto request)
    {
        try
        {
            var userId = GetUserIdFromToken(); // SINO ANG NAGSESEND? Kukunin natin sa Token!
            var result = await _friendService.SendFriendRequestAsync(userId, request);
            return Ok(new { message = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("accept/{friendshipId}")]
    public async Task<IActionResult> AcceptRequest(Guid friendshipId)
    {
        try
        {
            var result = await _friendService.AcceptFriendRequestAsync(friendshipId);
            return Ok(new { message = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("network")] // Wala na yung {userId} sa route!
    public async Task<IActionResult> GetMyNetwork()
    {
        // Matic na niyang kukunin yung network ng taong naka-login
        var network = await _friendService.GetUserNetworkAsync(GetUserIdFromToken());
        return Ok(network);
    }

    [HttpDelete("{friendshipId}")]
    public async Task<IActionResult> RemoveFriend(Guid friendshipId)
    {
        try
        {
            var message = await _friendService.RemoveFriendshipAsync(friendshipId);
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}