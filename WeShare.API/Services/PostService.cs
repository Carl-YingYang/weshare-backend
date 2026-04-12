using WeShare.API.Data; // 🚨 IDINAGDAG: Wag kalimutan ito!
using WeShare.API.DTOs;
using WeShare.API.Models;
using WeShare.API.Repositories;

namespace WeShare.API.Services;

public interface IPostService
{
    Task<IEnumerable<PostResponse>> GetAllPostsAsync();
    Task<PostResponse> CreatePostAsync(Guid userId, CreatePostRequest request);
    Task<string> ToggleLikeAsync(Guid userId, Guid postId);
    Task<CommentResponse> AddCommentAsync(Guid userId, Guid postId, CreateCommentRequest request);
}

public class PostService : IPostService
{
    private readonly IPostRepository _postRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationService _notifService;
    private readonly AppDbContext _context; // 🚨 IDINAGDAG NATIN ITO

    public PostService(IPostRepository postRepo, IUserRepository userRepo, INotificationService notifService, AppDbContext context)
    {
        _postRepo = postRepo;
        _userRepo = userRepo;
        _notifService = notifService;
        _context = context; // 🚨 SINI-SAVE NATIN DITO
    }

    public async Task<IEnumerable<PostResponse>> GetAllPostsAsync()
    {
        var posts = await _postRepo.GetAllPostsAsync();

        return posts.Select(p => new PostResponse(
            p.Id, p.Content, p.HasImage, p.ImageUrl, p.CreatedAt,
            p.User?.Username ?? "Unknown User", p.User?.Initials ?? "??", p.User?.ProfilePicture,
            p.Likes.Count, p.Comments.Count,
            p.Comments.Where(c => c.ParentCommentId == null).Select(c => new CommentResponse(
                c.Id, c.Content, c.CreatedAt,
                c.User?.Username ?? "Unknown", c.User?.Initials ?? "??", c.User?.ProfilePicture,
                null,
                p.Comments.Where(r => r.ParentCommentId == c.Id).Select(r => new CommentResponse(
                    r.Id, r.Content, r.CreatedAt,
                    r.User?.Username ?? "Unknown", r.User?.Initials ?? "??", r.User?.ProfilePicture,
                    r.ParentCommentId, null
                )).OrderBy(r => r.CreatedAt).ToList()
            )).OrderBy(c => c.CreatedAt).ToList()
        ));
    }

    public async Task<PostResponse> CreatePostAsync(Guid userId, CreatePostRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        var post = new Post
        {
            UserId = userId,
            Content = request.Content,
            HasImage = request.HasImage,
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        await _postRepo.AddAsync(post);

        return new PostResponse(
            post.Id, post.Content, post.HasImage, post.ImageUrl, post.CreatedAt,
            user.Username, user.Initials, user.ProfilePicture, 0, 0, new List<CommentResponse>()
        );
    }

    public async Task<string> ToggleLikeAsync(Guid userId, Guid postId)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post == null) throw new Exception("Post not found.");

        var existingLike = post.Likes.FirstOrDefault(l => l.UserId == userId);

        if (existingLike != null)
        {
            await _postRepo.RemoveLikeAsync(existingLike);
            return "Post unliked.";
        }
        else
        {
            var like = new PostLike { PostId = postId, UserId = userId };
            await _postRepo.AddLikeAsync(like);

            if (post.UserId != userId)
                await _notifService.CreateNotificationAsync(post.UserId, "Someone liked your post.", "Like");

            return "Post liked.";
        }
    }

    public async Task<CommentResponse> AddCommentAsync(Guid userId, Guid postId, CreateCommentRequest request)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post == null) throw new Exception("Post not found.");

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

        // 🚨 FIX: TAMA NA ANG LOGIC PARA SA REPLY NOTIFICATION 🚨
        if (request.ParentCommentId.HasValue)
        {
            // Kukunin natin sa DbContext yung comment para malaman sino nire-replyan mo
            var parentComment = await _context.Comments.FindAsync(request.ParentCommentId.Value);

            if (parentComment != null && parentComment.UserId != userId)
            {
                await _notifService.CreateNotificationAsync(parentComment.UserId, $"{user!.Username} replied to your comment.", "Comment");
            }
        }
        else
        {
            if (post.UserId != userId)
            {
                await _notifService.CreateNotificationAsync(post.UserId, $"{user!.Username} commented on your post.", "Comment");
            }
        }

        return new CommentResponse(
            comment.Id, comment.Content, comment.CreatedAt,
            user!.Username, user.Initials, user.ProfilePicture, comment.ParentCommentId, null
        );
    }
}