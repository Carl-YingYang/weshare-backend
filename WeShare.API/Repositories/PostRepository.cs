using Microsoft.EntityFrameworkCore;
using WeShare.API.Data;
using WeShare.API.Models;

namespace WeShare.API.Repositories;

public interface IPostRepository
{
    Task<IEnumerable<Post>> GetAllPostsAsync();
    Task<Post?> GetByIdAsync(Guid id);
    Task AddAsync(Post post);

    // MGA BAGONG METHODS PARA SA ENGAGEMENT
    Task<PostLike?> GetLikeAsync(Guid postId, Guid userId);
    Task AddLikeAsync(PostLike like);
    Task RemoveLikeAsync(PostLike like);
    Task AddCommentAsync(Comment comment);
}

public class PostRepository : IPostRepository
{
    private readonly AppDbContext _context;
    public PostRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Post>> GetAllPostsAsync()
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)     // <-- I-load ang Likes
            .Include(p => p.Comments)  // <-- I-load ang Comments
                .ThenInclude(c => c.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Post?> GetByIdAsync(Guid id)
    {
        return await _context.Posts.FindAsync(id);
    }

    public async Task AddAsync(Post post)
    {
        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────
    // ENGAGEMENT METHODS
    // ─────────────────────────────────────────────
    public async Task<PostLike?> GetLikeAsync(Guid postId, Guid userId)
    {
        return await _context.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
    }

    public async Task AddLikeAsync(PostLike like)
    {
        await _context.PostLikes.AddAsync(like);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveLikeAsync(PostLike like)
    {
        _context.PostLikes.Remove(like);
        await _context.SaveChangesAsync();
    }

    public async Task AddCommentAsync(Comment comment)
    {
        await _context.Comments.AddAsync(comment);
        await _context.SaveChangesAsync();
    }
}