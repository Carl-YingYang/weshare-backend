using Microsoft.AspNetCore.Http; // 🚨 REQUIRED FOR IFormFile
using Microsoft.AspNetCore.Mvc;
using WeShare.API.Models;
namespace WeShare.API.DTOs;

// 🚨 FIX: Only ONE definition of CreatePostRequest exists now
public class CreatePostRequest
{
    [FromForm(Name = "content")]
    public string Content { get; set; } = string.Empty;

    [FromForm(Name = "image")]
    public IFormFile? Image { get; set; }

    // 🚨 ADD THIS: 0 = Public, 1 = FriendsOnly, 2 = Private
    [FromForm(Name = "visibility")]
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
}

public record CreateCommentRequest(string Content, Guid? ParentCommentId = null);

public record PostResponse(
    Guid Id,
    string Content,
    bool HasImage,
    string? ImageUrl,
    DateTime CreatedAt,
    string AuthorName,
    string AuthorInitials,
    string? AuthorProfilePicture,
    int LikesCount,
    int CommentsCount,
    IEnumerable<CommentResponse> Comments
);

public record CommentResponse(
    Guid Id,
    string Content,
    DateTime CreatedAt,
    string AuthorName,
    string AuthorInitials,
    string? AuthorProfilePicture,
    Guid? ParentCommentId,
    IEnumerable<CommentResponse>? Replies = null
);
public class EditPostRequest
{
    // Make sure to add [FromForm] or [FromBody] depending on how you send it
    public string Content { get; set; } = string.Empty;
}