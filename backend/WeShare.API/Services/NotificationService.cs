using WeShare.API.DTOs;
using WeShare.API.Models;
using WeShare.API.Repositories;

namespace WeShare.API.Services;

public interface INotificationService
{
    Task CreateNotificationAsync(Guid userId, string content, string type);
    Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifRepo;
    public NotificationService(INotificationRepository notifRepo) => _notifRepo = notifRepo;

    public async Task CreateNotificationAsync(Guid userId, string content, string type)
    {
        var notif = new Notification { UserId = userId, Content = content, Type = type };
        await _notifRepo.AddAsync(notif);
    }

    public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId)
    {
        var notifs = await _notifRepo.GetUserNotificationsAsync(userId);
        return notifs.Select(n => new NotificationResponse(n.Id, n.Content, n.Type, n.IsRead, n.CreatedAt));
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        await _notifRepo.MarkAsReadAsync(notificationId);
    }
}