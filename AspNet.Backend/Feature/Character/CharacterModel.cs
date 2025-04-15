using System.Numerics;
using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Shared;

namespace AspNet.Backend.Feature.Character;

/// <summary>
/// The <see cref="CharacterModel"/> class
/// acts as the data model of the player.
/// </summary>
public class CharacterModel
{
    public int? Id { get; set; }
    public required string Type { get; set; }
    public required string Username { get; set; }
    public required TransformModel TransformModel { get; set; }
    public required User User { get; set; }
}