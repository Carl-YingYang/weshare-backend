namespace WeShare.API.Models;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── MGA BAGONG FIELDS PARA SA REPLY ──
    public Guid? ParentCommentId { get; set; } // Null kung main comment, may laman kung reply

    public Post? Post { get; set; }
    public User? User { get; set; }

    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}