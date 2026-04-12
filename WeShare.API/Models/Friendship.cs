namespace WeShare.API.Models;

public class Friendship
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Ang nag-send ng friend request
    public Guid RequesterId { get; set; }

    // Ang nakatanggap ng friend request
    public Guid ReceiverId { get; set; }

    // True kapag in-accept na, False kapag pending pa
    public bool IsAccepted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties para madali nating makuha yung User details nila
    public User? Requester { get; set; }
    public User? Receiver { get; set; }
}