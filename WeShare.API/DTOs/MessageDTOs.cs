namespace WeShare.API.DTOs;

public record MessageResponse(
    Guid Id,
    Guid SenderId,
    Guid ReceiverId,
    string Content,
    DateTime SentAt,
    Guid? ReplyToMessageId,      // <--- IDINAGDAG
    string? ReplyToContent       // <--- IDINAGDAG
);