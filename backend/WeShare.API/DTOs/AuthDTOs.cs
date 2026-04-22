namespace WeShare.API.DTOs;

// 1. What React sends when a user logs in
public record LoginRequest(string Email, string Password);

// 2. What React sends when someone creates an account
public record RegisterRequest(string Email, string Username, string Password, string Role, string Initials);

// 3. What we send BACK to React after a successful login
public record AuthResponse(string Token, UserDto User);

// 4. A safe version of the User without the password (NA-UPDATE NA MAY IMAGES!)
public record UserDto(
    Guid Id,
    string Email,
    string Username,
    string Role,
    string Initials,
    string? ProfilePicture,
    string? CoverPhoto
);