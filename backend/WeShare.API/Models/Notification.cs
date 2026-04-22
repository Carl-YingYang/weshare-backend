using System.ComponentModel.DataAnnotations.Schema;

namespace WeShare.API.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; } // Kung sino ang makaka-receive
    public string Content { get; set; } = string.Empty; // e.g., "Mikee liked your post"
    public string Type { get; set; } = string.Empty; // "Welcome", "Like", "Comment", "FriendRequest", "AcceptRequest"
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User? User { get; set; }
}