namespace SecretSanta.Web.Models;

public sealed class UserGiftDto
{
    public int UserGiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = string.Empty;
}

public sealed class ReceiverGiftDto
{
    // Property names match the current API response contract.
    public string RecieverName { get; set; } = string.Empty;
    public List<UserGiftDto> RecieverGifts { get; set; } = [];
}

public sealed class GameListDto
{
    public int GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsDrawn { get; set; }
    public bool IsAnonymous { get; set; }
    public int CreatorId { get; set; }
    public int GiftCost { get; set; }
    public List<string> Participants { get; set; } = [];
}

public sealed class CreateGameRequest
{
    public string Name { get; set; } = string.Empty;
    public int GiftCost { get; set; }
}

internal sealed class InviteResult
{
    public string InviteToken { get; set; } = string.Empty;
}
