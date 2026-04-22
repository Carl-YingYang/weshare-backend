using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeShare.API.DTOs;
using WeShare.API.Services;

namespace WeShare.API.Controllers;

/// <summary>
/// Handles HTTP requests for managing the social network, including sending, accepting, and removing friend requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Enforces that all endpoints require a valid JWT token
public class FriendsController : ControllerBase
{
    #region Initialization & Helpers
    private readonly IFriendshipService _friendService;

    public FriendsController(IFriendshipService friendService)
    {
        _friendService = friendService;
    }

    /// <summary>
    /// Securely extracts the current user's ID from their JWT token.
    /// </summary>
    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }
    #endregion

    #region Network Visualization

    /// <summary>
    /// Retrieves the social graph for the currently authenticated user.
    /// </summary>
    [HttpGet("network")]
    public async Task<IActionResult> GetMyNetwork()
    {
        // Contextual resolution: Automatically scopes the data to the token holder
        var network = await _friendService.GetUserNetworkAsync(GetUserIdFromToken());
        return Ok(network);
    }

    #endregion

    #region Connection Management

    /// <summary>
    /// Dispatches a new friend request to a target user.
    /// </summary>
    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var result = await _friendService.SendFriendRequestAsync(userId, request);
            return Ok(new { message = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Accepts a pending friend request sent to the authenticated user.
    /// </summary>
    [HttpPost("accept/{friendshipId}")]
    public async Task<IActionResult> AcceptRequest(Guid friendshipId)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var result = await _friendService.AcceptFriendRequestAsync(userId, friendshipId);
            return Ok(new { message = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Severs an active friendship or withdraws a pending request.
    /// </summary>
    [HttpDelete("{friendshipId}")]
    public async Task<IActionResult> RemoveFriend(Guid friendshipId)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var message = await _friendService.RemoveFriendshipAsync(userId, friendshipId);
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion
}