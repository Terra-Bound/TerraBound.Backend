using AspNet.Backend.Feature.Character;
using AspNet.Backend.Feature.Shared;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.Chunk;

/// <summary>
/// The <see cref="ChunkDto"/> struct
/// represents an <see cref="ChunkModel"/> with all his transferable data.
/// </summary>
public struct ChunkDto
{
    public int Id { get; set; }
    public string Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public DateTime CreatedDate { get; set; }
    public required ISet<CharacterDto> Characters { get; set; }
}

/// <summary>
/// The <see cref="ChunkModel"/> class
/// acts as the data model of the chunk.
/// </summary>
public class ChunkModel
{
    public int IdentityId { get; set; }
    public required IdentityModel Identity { get; set; }
    
    public int X { get; set; }
    public int Y { get; set; }
    
    public DateTime CreatedDate { get; set; }
  
    public ISet<CharacterModel> Characters { get; set; } = new HashSet<CharacterModel>(); 
}