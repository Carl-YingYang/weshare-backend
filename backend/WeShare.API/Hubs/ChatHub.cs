using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WeShare.API.Data;
using WeShare.API.Models;

namespace WeShare.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    public ChatHub(AppDbContext context) => _context = context;

    // ── NA-UPDATE: MAY REPLY ID NA ──
    public async Task SendMessage(string receiverIdStr, string content, string? replyToIdStr)
    {
        var senderIdStr = Context.UserIdentifier;
        if (senderIdStr == null || !Guid.TryParse(receiverIdStr, out var receiverId)) return;

        var senderId = Guid.Parse(senderIdStr);
        Guid? replyId = !string.IsNullOrEmpty(replyToIdStr) ? Guid.Parse(replyToIdStr) : null;

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            ReplyToMessageId = replyId,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Kunin yung text ng nire-replyan para lumabas agad sa ka-chat mo
        string? replyContent = null;
        if (replyId != null)
        {
            var replyMsg = await _context.Messages.FindAsync(replyId);
            replyContent = replyMsg?.Content;
        }

        var payload = new
        {
            id = message.Id,
            senderId,
            receiverId,
            content,
            sentAt = message.SentAt,
            replyToMessageId = replyId,
            replyToContent = replyContent
        };

        await Clients.User(receiverIdStr).SendAsync("ReceiveMessage", payload);
        await Clients.Caller.SendAsync("ReceiveMessage", payload);
    }

    // ── BAGONG FUNCTION: DELETE MESSAGE ──
    public async Task DeleteMessage(string messageIdStr, string receiverIdStr)
    {
        if (Guid.TryParse(messageIdStr, out var messageId))
        {
            var msg = await _context.Messages.FindAsync(messageId);
            if (msg != null && msg.SenderId.ToString() == Context.UserIdentifier)
            {
                _context.Messages.Remove(msg);
                await _context.SaveChangesAsync();

                // I-broadcast sa inyong dalawa na may na-delete
                await Clients.User(receiverIdStr).SendAsync("MessageDeleted", messageIdStr);
                await Clients.Caller.SendAsync("MessageDeleted", messageIdStr);
            }
        }
    }
}