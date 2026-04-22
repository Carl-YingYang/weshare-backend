namespace WeShare.API.DTOs;

// Notice: Wala na rin yung RequesterId dito! Hihingin na lang natin kung SINO ang ia-add nila.
public record SendFriendRequestDto(Guid ReceiverId);

public record FriendDto(
    Guid FriendshipId,
    Guid FriendUserId,
    string FriendName,
    string FriendInitials,
    string FriendRole,
    bool IsAccepted,
    bool IsRequester
);
