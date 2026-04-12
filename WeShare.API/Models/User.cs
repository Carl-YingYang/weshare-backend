namespace WeShare.API.Models;

public class User
{
    // Guid is a unique random string (better for security than 1, 2, 3)
    public Guid Id { get; set; } = Guid.NewGuid();

    // Matches the "Work Email" input in your React Login UI
    public string Email { get; set; } = string.Empty;

    // We store a scrambled version of the password for security
    public string PasswordHash { get; set; } = string.Empty;

    // Matches "Jakob Botosh" in your UI navbar
    public string Username { get; set; } = string.Empty;

    // Matches the "UI Architect" sub-text in your UI
    public string Role { get; set; } = "Member";

    // The initials for the Avatar bubble (e.g., "JB")
    public string Initials { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: One User can have many Posts
    public ICollection<Post> Posts { get; set; } = new List<Post>();

    public string? ProfilePicture { get; set; }
    public string? CoverPhoto { get; set; }
}