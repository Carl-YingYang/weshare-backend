using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeShare.API.DTOs;
using WeShare.API.Services;

namespace WeShare.API.Controllers;

/// <summary>
/// Handles all HTTP requests related to the Post lifecycle, Feeds, and User Engagement.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    #region Initialization & Helpers
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
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

    #region Discovery & Feeds

    /// <summary>
    /// Retrieves the global public feed. Accessible to guests without a token.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeed([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageSize > 50) pageSize = 50;
        if (pageNumber < 1) pageNumber = 1;

        var posts = await _postService.GetFeedAsync(pageNumber, pageSize);
        return Ok(posts);
    }

    /// <summary>
    /// Retrieves the personalized feed containing posts from the user and their accepted friends.
    /// </summary>
    [HttpGet("friends-feed")]
    public async Task<IActionResult> GetFriendsFeed()
    {
        try
        {
            var userId = GetUserIdFromToken();
            var posts = await _postService.GetFriendsFeedAsync(userId);
            return Ok(posts);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Post Management (CRUD)

    [HttpPost]
    public async Task<IActionResult> CreatePost(CreatePostRequest dto)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var response = await _postService.CreatePostAsync(userId, dto);
            return StatusCode(201, response); // 201 Created is the standard for POST creation
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{postId}")]
    public async Task<IActionResult> EditPost(Guid postId, [FromBody] EditPostRequest request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var response = await _postService.EditPostAsync(userId, postId, request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message }); // 403 Forbidden for ownership failures
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{postId}")]
    public async Task<IActionResult> DeletePost(Guid postId)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var message = await _postService.DeletePostAsync(userId, postId);
            return Ok(new { message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Engagement (Likes & Comments)

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
            var inner = ex.InnerException != null ? ex.InnerException.Message : "";
            return BadRequest(new { message = ex.Message, details = inner });
        }
    }

    [HttpGet("{postId}/comments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(Guid postId)
    {
        try
        {
            var comments = await _postService.GetCommentsByPostIdAsync(postId);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("comments/{commentId}")]
    public async Task<IActionResult> EditComment(Guid commentId, [FromBody] CreateCommentRequest request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var response = await _postService.EditCommentAsync(userId, commentId, request.Content);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        try
        {
            var userId = GetUserIdFromToken();
            await _postService.DeleteCommentAsync(userId, commentId);
            return Ok(new { message = "Comment deleted." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion
}