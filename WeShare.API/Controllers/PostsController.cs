using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeShare.API.DTOs;
using WeShare.API.Services;

namespace WeShare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 🔒 SECURED!
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }

    [HttpGet]
    public async Task<IActionResult> GetFeed()
    {
        var posts = await _postService.GetAllPostsAsync();
        return Ok(posts);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var response = await _postService.CreatePostAsync(userId, request);
            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─────────────────────────────────────────────
    // BAGONG ENDPOINTS PARA SA ENGAGEMENT
    // ─────────────────────────────────────────────

    [HttpPost("{postId}/like")]
    public async Task<IActionResult> ToggleLike(Guid postId)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var message = await _postService.ToggleLikeAsync(userId, postId);
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{postId}/comment")]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] CreateCommentRequest request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var response = await _postService.AddCommentAsync(userId, postId, request);
            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}