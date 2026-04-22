using WeShare.API.DTOs;
using WeShare.API.Models;
using WeShare.API.Repositories;

namespace WeShare.API.Services;

/// <summary>
/// Manages the social graph, handling the secure creation, acceptance, and removal of friend connections.
/// </summary>
public interface IFriendshipService
{
    #region Request Management
    Task<string> SendFriendRequestAsync(Guid requesterId, SendFriendRequestDto request);
    Task<string> AcceptFriendRequestAsync(Guid currentUserId, Guid friendshipId);
    Task<string> RemoveFriendshipAsync(Guid currentUserId, Guid friendshipId);
    #endregion

    #region Network Visualization
    Task<IEnumerable<FriendDto>> GetUserNetworkAsync(Guid userId);
    #endregion
}

public class FriendshipService : IFriendshipService
{
    #region Initialization
    private readonly IFriendshipRepository _friendRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationService _notifService;

    public FriendshipService(IFriendshipRepository friendRepo, IUserRepository userRepo, INotificationService notifService)
    {
        _friendRepo = friendRepo;
        _userRepo = userRepo;
        _notifService = notifService;
    }
    #endregion

    #region Request Management

    /// <summary>
    /// Initiates a new connection request. Validates against self-requests and duplicate connections.
    /// </summary>
    public async Task<string> SendFriendRequestAsync(Guid requesterId, SendFriendRequestDto request)
    {
        // Validation: Cannot befriend oneself
        if (requesterId == request.ReceiverId) throw new Exception("You cannot send a friend request to yourself.");

        var receiver = await _userRepo.GetByIdAsync(request.ReceiverId);
        if (receiver == null) throw new Exception("Target user does not exist.");

        // Validation: Prevent spamming requests to the same person
        var existingFriendship = await _friendRepo.GetFriendshipAsync(requesterId, request.ReceiverId);
        if (existingFriendship != null) throw new Exception("A friend request already exists or you are already friends.");

        var friendship = new Friendship { RequesterId = requesterId, ReceiverId = request.ReceiverId, IsAccepted = false };
        await _friendRepo.AddAsync(friendship);

        // Notify the target user
        await _notifService.CreateNotificationAsync(request.ReceiverId, "You have a new friend request.", "FriendRequest");

        return "Friend request sent successfully.";
    }

    /// <summary>
    /// Approves a pending request. Strictly enforces that only the intended Receiver can accept.
    /// </summary>
    public async Task<string> AcceptFriendRequestAsync(Guid currentUserId, Guid friendshipId)
    {
        var friendship = await _friendRepo.GetByIdAsync(friendshipId);
        if (friendship == null) throw new Exception("Friend request not found.");

        // 🚨 AUTHORIZATION GATE: Prevent forced acceptance by the requester or third parties
        if (friendship.ReceiverId != currentUserId)
            throw new UnauthorizedAccessException("You are not authorized to accept this request.");

        if (friendship.IsAccepted) throw new Exception("You are already friends.");

        friendship.IsAccepted = true;
        await _friendRepo.UpdateAsync(friendship);

        // Notify the requester that their request was approved
        await _notifService.CreateNotificationAsync(friendship.RequesterId, "Your friend request was accepted.", "AcceptRequest");

        return "Friend request accepted!";
    }

    /// <summary>
    /// Handles Unfriending or Canceling a request. Verifies that the acting user is part of the relationship.
    /// </summary>
    public async Task<string> RemoveFriendshipAsync(Guid currentUserId, Guid friendshipId)
    {
        var friendship = await _friendRepo.GetByIdAsync(friendshipId);
        if (friendship == null) throw new Exception("Connection not found.");

        // 🚨 AUTHORIZATION GATE: Only the two participants have the right to sever the connection
        if (friendship.RequesterId != currentUserId && friendship.ReceiverId != currentUserId)
            throw new UnauthorizedAccessException("You are not authorized to remove this connection.");

        await _friendRepo.DeleteAsync(friendship);
        return "Connection removed successfully.";
    }

    #endregion

    #region Network Visualization

    /// <summary>
    /// Flattens the relational database graph into a list of DTOs for frontend display.
    /// Determines whether the current user was the requester or receiver for UI context.
    /// </summary>
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
                network.Add(new FriendDto(
                    f.Id,
                    friendUser.Id,
                    friendUser.Username,
                    friendUser.Initials,
                    friendUser.Role,
                    f.IsAccepted,
                    isRequester));
            }
        }
        return network;
    }

    #endregion
}