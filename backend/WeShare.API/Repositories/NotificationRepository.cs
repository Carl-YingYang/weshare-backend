using Microsoft.EntityFrameworkCore;
using WeShare.API.Data;
using WeShare.API.Models;

namespace WeShare.API.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
}

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;
    public NotificationRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20) // Kukunin lang ang latest 20 para mabilis
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notif = await _context.Notifications.FindAsync(notificationId);
        if (notif != null)
        {
            notif.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}