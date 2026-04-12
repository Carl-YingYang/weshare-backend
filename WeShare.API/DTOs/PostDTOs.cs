namespace WeShare.API.DTOs;

public record CreatePostRequest(string Content, bool HasImage, string? ImageUrl = null);

public record CreateCommentRequest(string Content, Guid? ParentCommentId = null);

public record PostResponse(
    Guid Id,
    string Content,
    bool HasImage,
    string? ImageUrl, // <--- PANG-APAT
    DateTime CreatedAt, // <--- PANG-LIMA (Dito nag-error kanina kasi wala yung ImageUrl)
    string AuthorName,
    string AuthorInitials,
    string? AuthorProfilePicture,
    int LikesCount,
    int CommentsCount,
    IEnumerable<CommentResponse> Comments
);

public record CommentResponse(
    Guid Id,
    string Content,
    DateTime CreatedAt,
    string AuthorName,
    string AuthorInitials,
    string? AuthorProfilePicture,
    Guid? ParentCommentId,
    IEnumerable<CommentResponse>? Replies = null
);