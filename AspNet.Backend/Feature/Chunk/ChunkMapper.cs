using Arch.Core;
using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Character;
using AspNet.Backend.Feature.Shared;
using Riok.Mapperly.Abstractions;
using Identity = TerraBound.Core.Components.Identity;
using NetworkedTransform = TerraBound.Core.Components.NetworkedTransform;

namespace AspNet.Backend.Feature.Chunk;

using Character = TerraBound.Core.Components.Character;

#pragma warning disable RMG020

/// <summary>
/// The <see cref="ChunkMapper"/> class
/// defines a mapper that automatically generates code to convert and update between the <see cref="ChunkModel"/> and his Dtos.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
[UseStaticMapper(typeof(CharacterMapper))]
public static partial class ChunkMapper
{
    /// <summary>
    /// Converts an <see cref="ChunkModel"/> to its <see cref="ChunkDto"/>.
    /// </summary>
    /// <param name="chunk">The <see cref="ChunkModel"/>.</param>
    /// <returns>The converted <see cref="ChunkDto"/>.</returns>
    [MapProperty(nameof(ChunkModel.IdentityId), nameof(ChunkDto.Id))]
    [MapProperty(nameof(ChunkModel.Identity.Type), nameof(ChunkDto.Type))]
    public static partial ChunkDto ToDto(this ChunkModel chunk);
    
    /// <summary>
    /// Converts multiple components to its <see cref="CharacterDto"/>.
    /// </summary>
    /// <param name="identity">Its <see cref="Identity"/>.</param>
    /// <param name="chunk">Its <see cref="Character"/>.</param>
    /// <param name="transform">Its <see cref="NetworkedTransform"/>.</param>
    /// <returns>The converted <see cref="CharacterDto"/>.</returns>
    public static ChunkDto ToDto(Identity identity, TerraBound.Core.Components.Chunk chunk)
    {
        // Create dto
        return new ChunkDto
        {
            Id = identity.Id,
            Type = identity.Type,
            X = chunk.Grid.X,
            Y = chunk.Grid.Y,
            CreatedDate = DateTime.UtcNow,
            Characters = new HashSet<CharacterDto>(),
        };
    }
    
    /// <summary>
    /// Converts an <see cref="CharacterDto"/> to its <see cref="CharacterModel"/>.
    /// </summary>
    /// <param name="dto">The <see cref="CharacterDto"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <returns>The converted <see cref="CharacterModel"/>.</returns>
    [MapProperty(nameof(ChunkDto.Id), nameof(ChunkModel.IdentityId))]
    [MapProperty(nameof(ChunkDto.Type), nameof(ChunkModel.Identity.Type))]
    [MapValue(nameof(ChunkModel.Identity), Use = nameof(GetStubIdentity))]  // Alternative pass Identity and User via parameter to the ToEntity method
    public static partial ChunkModel ToEntity(this ChunkDto dto);

    /// <summary>
    /// Returns an empty <see cref="IdentityModel"/> since it is required in the constructor. 
    /// </summary>
    /// <returns>An empty <see cref="IdentityModel"/>.</returns>
    private static IdentityModel GetStubIdentity()
    {
        return new IdentityModel();
    }
}

#pragma warning restore RMG020