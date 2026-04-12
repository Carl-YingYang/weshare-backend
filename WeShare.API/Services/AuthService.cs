using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WeShare.API.DTOs;
using WeShare.API.Models;
using WeShare.API.Repositories;

namespace WeShare.API.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;
    private readonly INotificationService _notifService;

    public AuthService(IUserRepository userRepo, IConfiguration config, INotificationService notifService)
    {
        _userRepo = userRepo;
        _config = config;
        _notifService = notifService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepo.GetByEmailAsync(request.Email);
        if (existingUser != null)
            throw new Exception("Email already in use.");

        var newUser = new User
        {
            Email = request.Email,
            Username = request.Username,
            Role = request.Role,
            Initials = request.Initials,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _userRepo.AddAsync(newUser);

        var token = GenerateJwtToken(newUser);

        // GINAMIT NA NATIN ANG TAMA NA "UserDto"
        var userDto = new UserDto(
            newUser.Id,
            newUser.Email,
            newUser.Username,
            newUser.Role,
            newUser.Initials,
            newUser.ProfilePicture,
            newUser.CoverPhoto
        );

        // TOTOONG NOTIFICATION!
        await _notifService.CreateNotificationAsync(newUser.Id, "Welcome to WeShare! Build your network today.", "Welcome");

        return new AuthResponse(token, userDto);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = GenerateJwtToken(user);

        // GINAMIT NA NATIN ANG TAMA NA "UserDto" DITO RIN
        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.Username,
            user.Role,
            user.Initials,
            user.ProfilePicture,
            user.CoverPhoto
        );

        return new AuthResponse(token, userDto);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("username", user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}