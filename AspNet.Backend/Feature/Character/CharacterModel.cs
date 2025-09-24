using System.Numerics;
using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Shared;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.Character;

/// <summary>
/// The <see cref="CharacterDto"/> struct
/// represents an <see cref="CharacterModel"/> with all his transferable data.
/// </summary>
public struct CharacterDto
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string Username { get; set; }
    public NetworkedTransform Transform { get; set; }
    public string UserId { get; set; }
}

/// <summary>
/// The <see cref="CharacterModel"/> class
/// acts as the data model of the player.
/// </summary>
public class CharacterModel
{
    public int IdentityId { get; set; }
    public required IdentityModel Identity { get; set; }
    
    public required string Username { get; set; }
    public required TransformModel Transform { get; set; }
    
    public required string UserId { get; set; }
    public required User User { get; set; }
}