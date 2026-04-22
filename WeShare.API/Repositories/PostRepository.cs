using Microsoft.EntityFrameworkCore;
using WeShare.API.Data;
using WeShare.API.Models;

namespace WeShare.API.Repositories;

public interface IPostRepository
{
    #region Post Management
    Task AddAsync(Post post);
    Task UpdateAsync(Post post);
    Task DeleteAsync(Post post);
    #endregion

    #region Retrieval & Feeds
    Task<Post?> GetByIdAsync(Guid id);
    Task<IEnumerable<Post>> GetAllPostsAsync();
    Task<IEnumerable<Post>> GetFeedAsync(int pageNumber, int pageSize);
    Task<IEnumerable<Post>> GetFriendsFeedAsync(Guid userId);
    #endregion

    #region Engagement (Likes & Comments)
    Task<PostLike?> GetLikeAsync(Guid postId, Guid userId);
    Task AddLikeAsync(PostLike like);
    Task RemoveLikeAsync(PostLike like);
    Task AddCommentAsync(Comment comment);
    Task<Comment?> GetCommentByIdAsync(Guid id);
    Task UpdateCommentAsync(Comment comment);
    Task DeleteCommentAsync(Comment comment);
    Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(Guid postId);
    #endregion
}

public class PostRepository : IPostRepository
{
    private readonly AppDbContext _context;

    public PostRepository(AppDbContext context)
    {
        _context = context;
    }

    #region Post Management

    /// <summary>
    /// Persists a new post record to the database.
    /// </summary>
    public async Task AddAsync(Post post)
    {
        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an existing post's content or metadata.
    /// </summary>
    public async Task UpdateAsync(Post post)
    {
        _context.Posts.Update(post);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a post and its associated physical file references from the database.
    /// </summary>
    public async Task DeleteAsync(Post post)
    {
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Retrieval & Feeds

    /// <summary>
    /// Retrieves a single post by its ID, including like metadata for toggle logic.
    /// </summary>
    public async Task<Post?> GetByIdAsync(Guid id)
    {
        return await _context.Posts
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Returns all posts in descending chronological order. Use for administrative views.
    /// </summary>
    public async Task<IEnumerable<Post>> GetAllPostsAsync()
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Global Discovery Feed: Returns paginated public posts only.
    /// </summary>
    public async Task<IEnumerable<Post>> GetFeedAsync(int pageNumber, int pageSize)
    {
        int rowsToSkip = (pageNumber - 1) * pageSize;

        return await _context.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .Where(p => p.Visibility == PostVisibility.Public)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(rowsToSkip)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Personalized Feed: Returns posts from the user and their accepted friends.
    /// Filters out 'Private' content from friends.
    /// </summary>
    public async Task<IEnumerable<Post>> GetFriendsFeedAsync(Guid userId)
    {
        var friendIds = await _context.Friendships
            .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) && f.IsAccepted)
            .Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId)
            .ToListAsync();

        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .Where(p => p.UserId == userId || (friendIds.Contains(p.UserId) && p.Visibility != PostVisibility.Private))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    #endregion

    #region Engagement (Likes & Comments)

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

    public async Task<Comment?> GetCommentByIdAsync(Guid id)
    {
        return await _context.Comments.FindAsync(id);
    }

    public async Task UpdateCommentAsync(Comment comment)
    {
        _context.Comments.Update(comment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(Comment comment)
    {
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(Guid postId)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    #endregion
}