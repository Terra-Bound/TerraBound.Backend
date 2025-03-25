using AspNet.Backend.Feature.Shared;

namespace AspNet.Backend.Feature.Player;

/// <summary>
/// The <see cref="Player"/> class
/// acts as the data model of the player.
/// </summary>
public class Player
{
    public int? Id { get; set; }
    public required string Username { get; set; }
    public required string Type { get; set; }
}