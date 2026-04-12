using System.ComponentModel.DataAnnotations.Schema;

namespace WeShare.API.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // ── BAGONG FIELDS PARA SA REPLY ──
    public Guid? ReplyToMessageId { get; set; }

    [ForeignKey("ReplyToMessageId")]
    public Message? ReplyToMessage { get; set; }

    [ForeignKey("SenderId")]
    public User? Sender { get; set; }

    [ForeignKey("ReceiverId")]
    public User? Receiver { get; set; }
}