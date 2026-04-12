using Microsoft.EntityFrameworkCore;
using WeShare.API.Data;
using WeShare.API.Models;

namespace WeShare.API.Repositories;

public interface IFriendshipRepository
{
    Task<Friendship?> GetFriendshipAsync(Guid user1Id, Guid user2Id);
    Task<Friendship?> GetByIdAsync(Guid id);
    Task AddAsync(Friendship friendship);
    Task UpdateAsync(Friendship friendship);
    Task<IEnumerable<Friendship>> GetUserFriendshipsAsync(Guid userId);
    Task DeleteAsync(Friendship friendship);
}

public class FriendshipRepository : IFriendshipRepository
{
    private readonly AppDbContext _context;

    public FriendshipRepository(AppDbContext context)
    {
        _context = context;
    }

    // Chine-check kung magkaibigan na ba or may pending request na sila
    public async Task<Friendship?> GetFriendshipAsync(Guid user1Id, Guid user2Id)
    {
        return await _context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterId == user1Id && f.ReceiverId == user2Id) ||
                (f.RequesterId == user2Id && f.ReceiverId == user1Id));
    }

    public async Task AddAsync(Friendship friendship)
    {
        await _context.Friendships.AddAsync(friendship);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Friendship friendship)
    {
        _context.Friendships.Update(friendship);
        await _context.SaveChangesAsync();
    }

    // Kinukuha lahat ng friendship connections ng isang user (kasama na yung profile ng kaibigan nila)
    public async Task<IEnumerable<Friendship>> GetUserFriendshipsAsync(Guid userId)
    {
        return await _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Receiver)
            .Where(f => f.RequesterId == userId || f.ReceiverId == userId)
            .ToListAsync();
    }
    public async Task<Friendship?> GetByIdAsync(Guid id)
    {
        return await _context.Friendships.FindAsync(id);
    }
    public async Task DeleteAsync(Friendship friendship)
    {
        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync();
    }

}