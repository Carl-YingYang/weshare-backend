using Microsoft.AspNetCore.Http;

namespace WeShare.API.DTOs;

public record ProfileResponse(
    Guid Id, string Email, string Username, string Role, string Initials, DateTime JoinedAt, string? ProfilePicture, string? CoverPhoto);

public record UserResponse(
    Guid Id, string Email, string Username, string Role, string Initials, string? ProfilePicture, string? CoverPhoto);

// 🚨 FIX: Changed from 'string' to 'IFormFile' so it accepts actual binary files
public record UpdateProfileRequest(
    string Username,
    string Role,
    string Initials,
    IFormFile? ProfilePicture,
    IFormFile? CoverPhoto);