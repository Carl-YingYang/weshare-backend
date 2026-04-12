namespace WeShare.API.Models;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool HasImage { get; set; } = false;

    // 🚨 IDINAGDAG: Dito natin ise-save yung Base64 image data!
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}