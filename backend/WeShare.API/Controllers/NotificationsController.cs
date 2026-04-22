using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeShare.API.Services;

namespace WeShare.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifService;
    public NotificationsController(INotificationService notifService) => _notifService = notifService;

    // Helper method para kunin ang UserId mula sa JWT Token
    private Guid GetUserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var notifs = await _notifService.GetUserNotificationsAsync(GetUserId());
        return Ok(notifs);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _notifService.MarkAsReadAsync(id);
        return Ok(new { message = "Marked as read." });
    }
}