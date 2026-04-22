using WeShare.API.DTOs;
using WeShare.API.Repositories;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq; // 🚨 Added to fix the .Select() error
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WeShare.API.Services;

public interface IUserService
{
    Task<IEnumerable<UserResponse>> GetAllUsersAsync();
    Task<ProfileResponse?> GetProfileAsync(Guid id);
    Task<ProfileResponse> UpdateProfileAsync(Guid id, UpdateProfileRequest request);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IWebHostEnvironment _env;

    public UserService(IUserRepository userRepo, IWebHostEnvironment env)
    {
        _userRepo = userRepo;
        _env = env;
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
    {
        // 🚨 If this still errors, check Cause 2 above!
        var users = await _userRepo.GetAllAsync();
        return users.Select(u => new UserResponse(u.Id, u.Email, u.Username, u.Role, u.Initials, u.ProfilePicture, u.CoverPhoto));
    }

    public async Task<ProfileResponse?> GetProfileAsync(Guid id)
    {
        var u = await _userRepo.GetByIdAsync(id);
        if (u == null) return null;
        return new ProfileResponse(u.Id, u.Email, u.Username, u.Role, u.Initials, u.CreatedAt, u.ProfilePicture, u.CoverPhoto);
    }

    /// <summary>
    /// Updates user metadata and securely handles physical file uploads for profile and cover photos.
    /// </summary>
    public async Task<ProfileResponse> UpdateProfileAsync(Guid id, UpdateProfileRequest request)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) throw new Exception("User not found.");

        // Update Text Data
        user.Username = request.Username;
        user.Role = request.Role;
        user.Initials = request.Initials;

        // SECURE FILE HANDLING: Profile Picture
        if (request.ProfilePicture != null && request.ProfilePicture.Length > 0)
        {
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(request.ProfilePicture.FileName);
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.ProfilePicture.CopyToAsync(fileStream);
            }
            user.ProfilePicture = $"/uploads/{uniqueFileName}";
        }

        // SECURE FILE HANDLING: Cover Photo
        if (request.CoverPhoto != null && request.CoverPhoto.Length > 0)
        {
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(request.CoverPhoto.FileName);
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.CoverPhoto.CopyToAsync(fileStream);
            }
            user.CoverPhoto = $"/uploads/{uniqueFileName}";
        }

        await _userRepo.UpdateAsync(user);
        return new ProfileResponse(user.Id, user.Email, user.Username, user.Role, user.Initials, user.CreatedAt, user.ProfilePicture, user.CoverPhoto);
    }
}