using Microsoft.EntityFrameworkCore;
using WeShare.API.Data;
using WeShare.API.Models;

namespace WeShare.API.Repositories;

public interface IFriendshipRepository
{
    #region Friendship Management
    Task AddAsync(Friendship friendship);
    Task UpdateAsync(Friendship friendship);
    Task DeleteAsync(Friendship friendship);
    #endregion

    #region Connection Retrieval
    Task<Friendship?> GetByIdAsync(Guid id);
    Task<Friendship?> GetFriendshipAsync(Guid user1Id, Guid user2Id);
    Task<IEnumerable<Friendship>> GetUserFriendshipsAsync(Guid userId);
    #endregion
}

public class FriendshipRepository : IFriendshipRepository
{
    private readonly AppDbContext _context;

    public FriendshipRepository(AppDbContext context)
    {
        _context = context;
    }

    #region Friendship Management

    /// <summary>
    /// Records a new friend request in the database.
    /// </summary>
    public async Task AddAsync(Friendship friendship)
    {
        await _context.Friendships.AddAsync(friendship);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates the status of a friendship (e.g., from Pending to Accepted).
    /// </summary>
    public async Task UpdateAsync(Friendship friendship)
    {
        _context.Friendships.Update(friendship);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a friendship record (Unfriend or Withdraw Request).
    /// </summary>
    public async Task DeleteAsync(Friendship friendship)
    {
        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Connection Retrieval

    /// <summary>
    /// Fetches a specific friendship record by its primary key.
    /// </summary>
    public async Task<Friendship?> GetByIdAsync(Guid id)
    {
        return await _context.Friendships.FindAsync(id);
    }

    /// <summary>
    /// Determines if a relationship already exists between two users.
    /// </summary>
    public async Task<Friendship?> GetFriendshipAsync(Guid user1Id, Guid user2Id)
    {
        return await _context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterId == user1Id && f.ReceiverId == user2Id) ||
                (f.RequesterId == user2Id && f.ReceiverId == user1Id));
    }

    /// <summary>
    /// Retrieves the entire social network of a user, including the profiles of their friends.
    /// </summary>
    public async Task<IEnumerable<Friendship>> GetUserFriendshipsAsync(Guid userId)
    {
        return await _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Receiver)
            .Where(f => f.RequesterId == userId || f.ReceiverId == userId)
            .ToListAsync();
    }

    #endregion
}