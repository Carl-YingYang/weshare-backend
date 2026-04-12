using WeShare.API.DTOs;
using WeShare.API.Models;
using WeShare.API.Repositories;

namespace WeShare.API.Services;

public interface IFriendshipService
{
    Task<string> SendFriendRequestAsync(Guid requesterId, SendFriendRequestDto request);
    Task<string> AcceptFriendRequestAsync(Guid friendshipId);
    Task<string> RemoveFriendshipAsync(Guid friendshipId);
    Task<IEnumerable<FriendDto>> GetUserNetworkAsync(Guid userId);
}

public class FriendshipService : IFriendshipService
{
    private readonly IFriendshipRepository _friendRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationService _notifService; // <--- IDINAGDAG DITO

    // IDINAGDAG SA CONSTRUCTOR
    public FriendshipService(IFriendshipRepository friendRepo, IUserRepository userRepo, INotificationService notifService)
    {
        _friendRepo = friendRepo;
        _userRepo = userRepo;
        _notifService = notifService;
    }

    public async Task<string> SendFriendRequestAsync(Guid requesterId, SendFriendRequestDto request)
    {
        if (requesterId == request.ReceiverId) throw new Exception("You cannot send a friend request to yourself.");

        var receiver = await _userRepo.GetByIdAsync(request.ReceiverId);
        if (receiver == null) throw new Exception("Target user does not exist.");

        var existingFriendship = await _friendRepo.GetFriendshipAsync(requesterId, request.ReceiverId);
        if (existingFriendship != null) throw new Exception("A friend request already exists or you are already friends.");

        var friendship = new Friendship { RequesterId = requesterId, ReceiverId = request.ReceiverId, IsAccepted = false };
        await _friendRepo.AddAsync(friendship);

        // NOTIFICATION: Kapag may nag-add
        await _notifService.CreateNotificationAsync(request.ReceiverId, "You have a new friend request.", "FriendRequest");

        return "Friend request sent successfully.";
    }

    public async Task<string> AcceptFriendRequestAsync(Guid friendshipId)
    {
        var friendship = await _friendRepo.GetByIdAsync(friendshipId);
        if (friendship == null) throw new Exception("Friend request not found.");
        if (friendship.IsAccepted) throw new Exception("You are already friends.");

        friendship.IsAccepted = true;
        await _friendRepo.UpdateAsync(friendship);

        // NOTIFICATION: Kapag in-accept
        await _notifService.CreateNotificationAsync(friendship.RequesterId, "Your friend request was accepted.", "AcceptRequest");

        return "Friend request accepted!";
    }

    public async Task<string> RemoveFriendshipAsync(Guid friendshipId)
    {
        var friendship = await _friendRepo.GetByIdAsync(friendshipId);
        if (friendship == null) throw new Exception("Friend request or connection not found.");
        await _friendRepo.DeleteAsync(friendship);
        return "Connection removed successfully.";
    }

    public async Task<IEnumerable<FriendDto>> GetUserNetworkAsync(Guid userId)
    {
        var friendships = await _friendRepo.GetUserFriendshipsAsync(userId);
        var network = new List<FriendDto>();
        foreach (var f in friendships)
        {
            bool isRequester = f.RequesterId == userId;
            var friendUser = isRequester ? f.Receiver : f.Requester;
            if (friendUser != null)
            {
                network.Add(new FriendDto(f.Id, friendUser.Id, friendUser.Username, friendUser.Initials, friendUser.Role, f.IsAccepted, isRequester));
            }
        }
        return network;
    }
}