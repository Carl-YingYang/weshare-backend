using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeShare.API.Data;
using WeShare.API.DTOs;

namespace WeShare.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessagesController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("{friendId}")]
    public async Task<IActionResult> GetChatHistory(Guid friendId)
    {
        var myId = GetUserId();

        var messages = await _context.Messages
            .Include(m => m.ReplyToMessage) // I-include ang nire-replyan
            .Where(m => (m.SenderId == myId && m.ReceiverId == friendId) ||
                        (m.SenderId == friendId && m.ReceiverId == myId))
            .OrderBy(m => m.SentAt)
            // I-map pati ang ReplyToContent
            .Select(m => new MessageResponse(m.Id, m.SenderId, m.ReceiverId, m.Content, m.SentAt, m.ReplyToMessageId, m.ReplyToMessage != null ? m.ReplyToMessage.Content : null))
            .ToListAsync();

        return Ok(messages);
    }

    // ── BAGONG ENDPOINT: Bilangin ang hindi pa nababasang chat ──
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadMessagesCount()
    {
        var myId = GetUserId();
        var count = await _context.Messages.CountAsync(m => m.ReceiverId == myId && !m.IsRead);
        return Ok(new { count });
    }

    // ── BAGONG ENDPOINT: I-mark as read kapag binuksan na ang ChatBox ──
    [HttpPut("read/{senderId}")]
    public async Task<IActionResult> MarkAsRead(Guid senderId)
    {
        var myId = GetUserId();
        var unreadMessages = await _context.Messages
            .Where(m => m.SenderId == senderId && m.ReceiverId == myId && !m.IsRead)
            .ToListAsync();

        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }

        return Ok();
    }
}