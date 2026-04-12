namespace WeShare.API.DTOs;

// Ito yung ibabalik natin kapag may nag-view ng profile
public record ProfileResponse(
    Guid Id,
    string Email,
    string Username,
    string Role,
    string Initials,
    DateTime JoinedAt,
    string? ProfilePicture,
    string? CoverPhoto
);

// Ito yung ginagamit para sa "Suggested Friends"
public record UserResponse(
    Guid Id,
    string Email,
    string Username,
    string Role,
    string Initials,
    string? ProfilePicture,
    string? CoverPhoto
);

// Ito yung JSON na ipapadala natin galing React papuntang C# para mag-update ng profile at images
public record UpdateProfileRequest(
    string Username,
    string Role,
    string Initials,
    string? ProfilePicture,
    string? CoverPhoto
);