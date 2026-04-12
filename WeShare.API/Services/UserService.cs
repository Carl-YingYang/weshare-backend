using WeShare.API.DTOs;
using WeShare.API.Repositories;

namespace WeShare.API.Services;

public interface IUserService
{
    Task<ProfileResponse> GetProfileAsync(Guid userId);
    Task<ProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<IEnumerable<UserResponse>> GetAllUsersAsync();
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
    {
        var users = await _userRepo.GetAllUsersAsync();

        // Ipinasa na natin ang 7 parameters dito kasama ang images
        return users.Select(u => new UserResponse(
            u.Id,
            u.Email,
            u.Username,
            u.Role,
            u.Initials,
            u.ProfilePicture,
            u.CoverPhoto
        ));
    }

    public async Task<ProfileResponse> GetProfileAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        return new ProfileResponse(
            user.Id,
            user.Email,
            user.Username,
            user.Role,
            user.Initials,
            user.CreatedAt,
            user.ProfilePicture,
            user.CoverPhoto
        );
    }

    public async Task<ProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new Exception("User not found.");

        // I-update natin yung mga text fields
        user.Username = request.Username;
        user.Role = request.Role;
        user.Initials = request.Initials;

        // I-save natin ang images DITO SA TAMANG PWESTO
        if (request.ProfilePicture != null) user.ProfilePicture = request.ProfilePicture;
        if (request.CoverPhoto != null) user.CoverPhoto = request.CoverPhoto;

        await _userRepo.UpdateAsync(user);

        return new ProfileResponse(
            user.Id,
            user.Email,
            user.Username,
            user.Role,
            user.Initials,
            user.CreatedAt,
            user.ProfilePicture,
            user.CoverPhoto
        );
    }
}