namespace WeShare.API.DTOs;

public record NotificationResponse(
    Guid Id,
    string Content,
    string Type,
    bool IsRead,
    DateTime CreatedAt
);