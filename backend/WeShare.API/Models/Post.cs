namespace WeShare.API.Models;

public enum PostVisibility
{
    Public = 0,
    FriendsOnly = 1,
    Private = 2
}

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool HasImage { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PostVisibility Visibility { get; set; } = PostVisibility.Public;

    public User? User { get; set; }
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}