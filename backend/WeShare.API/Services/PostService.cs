using WeShare.API.DTOs;
using WeShare.API.Models;
using WeShare.API.Repositories;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace WeShare.API.Services;

/// <summary>
/// Defines the business contract for Post-related operations, including privacy and engagement.
/// </summary>
public interface IPostService
{
    #region Post Lifecycle
    Task<PostResponse> CreatePostAsync(Guid userId, CreatePostRequest request);
    Task<PostResponse> EditPostAsync(Guid userId, Guid postId, EditPostRequest request);
    Task<string> DeletePostAsync(Guid userId, Guid postId);
    #endregion

    #region Feed Retrieval
    Task<IEnumerable<PostResponse>> GetAllPostsAsync();
    Task<IEnumerable<PostResponse>> GetFeedAsync(int pageNumber, int pageSize);
    Task<IEnumerable<PostResponse>> GetFriendsFeedAsync(Guid userId);
    #endregion

    #region Engagement
    Task<string> ToggleLikeAsync(Guid userId, Guid postId);
    Task<CommentResponse> AddCommentAsync(Guid userId, Guid postId, CreateCommentRequest request);
    Task<CommentResponse> EditCommentAsync(Guid userId, Guid commentId, string newContent);
    Task DeleteCommentAsync(Guid userId, Guid commentId);
    Task<IEnumerable<CommentResponse>> GetCommentsByPostIdAsync(Guid postId);
    #endregion
}

public class PostService : IPostService
{
    #region Initialization
    private readonly IPostRepository _postRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationService _notifService;
    private readonly IWebHostEnvironment _env;
    private readonly IFriendshipRepository _friendRepo;

    public PostService(
        IPostRepository postRepo,
        IUserRepository userRepo,
        INotificationService notifService,
        IWebHostEnvironment env,
        IFriendshipRepository friendRepo)
    {
        _postRepo = postRepo;
        _userRepo = userRepo;
        _notifService = notifService;
        _env = env;
        _friendRepo = friendRepo;
    }
    #endregion

    #region Post Lifecycle

    /// <summary>
    /// Creates a new post. Handles physical image uploads and applies the requested visibility setting.
    /// </summary>
    public async Task<PostResponse> CreatePostAsync(Guid userId, CreatePostRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        string? imageUrl = null;
        if (request.Image != null && request.Image.Length > 0)
        {
            // Generates a unique filename to prevent overwriting existing files
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(request.Image.FileName);
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.Image.CopyToAsync(fileStream);
            }
            imageUrl = $"/uploads/{uniqueFileName}";
        }

        var post = new Post
        {
            UserId = userId,
            Content = request.Content,
            HasImage = request.Image != null,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
            Visibility = request.Visibility // Assigns Privacy Level (Public, FriendsOnly, Private)
        };

        await _postRepo.AddAsync(post);
        return MapToPostResponse(post);
    }

    /// <summary>
    /// Updates post content after verifying that the requester is the original author.
    /// </summary>
    public async Task<PostResponse> EditPostAsync(Guid userId, Guid postId, EditPostRequest request)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post == null) throw new Exception("Post not found.");

        // 🚨 AUTHORIZATION GATE: Prevent tampering by other users
        if (post.UserId != userId) throw new UnauthorizedAccessException("You can only edit your own posts.");

        post.Content = request.Content;
        await _postRepo.UpdateAsync(post);

        return MapToPostResponse(post);
    }

    /// <summary>
    /// Deletes a post from the database and scrubs the associated image file from the server.
    /// </summary>
    public async Task<string> DeletePostAsync(Guid userId, Guid postId)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post == null) throw new Exception("Post not found.");

        // 🚨 AUTHORIZATION GATE
        if (post.UserId != userId) throw new UnauthorizedAccessException("You can only delete your own posts.");

        // Clean up the physical image file to save server space
        if (!string.IsNullOrEmpty(post.ImageUrl))
        {
            var filePath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), post.ImageUrl.TrimStart('/'));
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        await _postRepo.DeleteAsync(post);
        return "Post deleted successfully.";
    }

    #endregion

    #region Feed Retrieval

    public async Task<IEnumerable<PostResponse>> GetAllPostsAsync()
    {
        var posts = await _postRepo.GetAllPostsAsync();
        return posts.Select(MapToPostResponse);
    }

    public async Task<IEnumerable<PostResponse>> GetFeedAsync(int pageNumber, int pageSize)
    {
        var posts = await _postRepo.GetFeedAsync(pageNumber, pageSize);
        return posts.Select(MapToPostResponse);
    }

    public async Task<IEnumerable<PostResponse>> GetFriendsFeedAsync(Guid userId)
    {
        var posts = await _postRepo.GetFriendsFeedAsync(userId);
        return posts.Select(MapToPostResponse);
    }

    #endregion

    #region Engagement (Likes & Comments)

    /// <summary>
    /// Toggles a post like ON or OFF. Enforces friendship privacy rules before executing.
    /// </summary>
    public async Task<string> ToggleLikeAsync(Guid userId, Guid postId)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post == null) throw new Exception("Post not found.");

        // 🚨 PRIVACY GATE: If Friends-Only, verify active friendship
        if (post.Visibility == PostVisibility.FriendsOnly && post.UserId != userId)
        {
            var friendship = await _friendRepo.GetFriendshipAsync(userId, post.UserId);
            if (friendship == null || !friendship.IsAccepted)
                throw new UnauthorizedAccessException("Only friends can like this post.");
        }

        var existingLike = post.Likes.FirstOrDefault(l => l.UserId == userId);
        if (existingLike != null)
        {
            await _postRepo.RemoveLikeAsync(existingLike);
            return "Post unliked.";
        }

        await _postRepo.AddLikeAsync(new PostLike { PostId = postId, UserId = userId });

        // Trigger notification if liking someone else's post
        if (post.UserId != userId)
            await _notifService.CreateNotificationAsync(post.UserId, "Someone liked your post.", "Like");

        return "Post liked.";
    }

    /// <summary>
    /// Adds a comment or reply to a post. Enforces privacy rules.
    /// </summary>
    public async Task<CommentResponse> AddCommentAsync(Guid userId, Guid postId, CreateCommentRequest request)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post == null) throw new Exception("Post not found.");

        // 🚨 PRIVACY GATE
        if (post.Visibility == PostVisibility.FriendsOnly && post.UserId != userId)
        {
            var friendship = await _friendRepo.GetFriendshipAsync(userId, post.UserId);
            if (friendship == null || !friendship.IsAccepted)
                throw new UnauthorizedAccessException("Only friends can comment on this post.");
        }

        var user = await _userRepo.GetByIdAsync(userId);
        var comment = new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = request.Content,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        await _postRepo.AddCommentAsync(comment);

        return new CommentResponse(comment.Id, comment.Content, comment.CreatedAt, user!.Username, user.Initials, user.ProfilePicture, comment.ParentCommentId, null);
    }

    public async Task<CommentResponse> EditCommentAsync(Guid userId, Guid commentId, string newContent)
    {
        var comment = await _postRepo.GetCommentByIdAsync(commentId);
        if (comment == null) throw new Exception("Comment not found.");

        if (comment.UserId != userId) throw new UnauthorizedAccessException("You can only edit your own comments.");

        comment.Content = newContent;
        await _postRepo.UpdateCommentAsync(comment);

        var user = await _userRepo.GetByIdAsync(userId);
        return new CommentResponse(comment.Id, comment.Content, comment.CreatedAt, user!.Username, user.Initials, user.ProfilePicture, comment.ParentCommentId, null);
    }

    public async Task DeleteCommentAsync(Guid userId, Guid commentId)
    {
        var comment = await _postRepo.GetCommentByIdAsync(commentId);
        if (comment == null) throw new Exception("Comment not found.");

        if (comment.UserId != userId) throw new UnauthorizedAccessException("You can only delete your own comments.");

        await _postRepo.DeleteCommentAsync(comment);
    }

    public async Task<IEnumerable<CommentResponse>> GetCommentsByPostIdAsync(Guid postId)
    {
        var comments = await _postRepo.GetCommentsByPostIdAsync(postId);
        return comments.Select(c => new CommentResponse(c.Id, c.Content, c.CreatedAt, c.User?.Username ?? "Unknown", c.User?.Initials ?? "??", c.User?.ProfilePicture, c.ParentCommentId, null)).OrderBy(c => c.CreatedAt);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Centralized mapping logic to convert Database Entities into UI-ready DTOs.
    /// Handles recursive nesting for comment replies.
    /// </summary>
    private PostResponse MapToPostResponse(Post p)
    {
        return new PostResponse(
            p.Id, p.Content, p.HasImage, p.ImageUrl, p.CreatedAt,
            p.User?.Username ?? "Unknown User", p.User?.Initials ?? "??", p.User?.ProfilePicture,
            p.Likes.Count, p.Comments.Count,
            p.Comments.Where(c => c.ParentCommentId == null).Select(c => new CommentResponse(
                c.Id, c.Content, c.CreatedAt, c.User?.Username ?? "Unknown", c.User?.Initials ?? "??", c.User?.ProfilePicture, c.ParentCommentId,
                p.Comments.Where(r => r.ParentCommentId == c.Id).Select(r => new CommentResponse(
                    r.Id, r.Content, r.CreatedAt, r.User?.Username ?? "Unknown", r.User?.Initials ?? "??", r.User?.ProfilePicture, r.ParentCommentId, null
                )).ToList()
            )).ToList()
        );
    }

    #endregion
}