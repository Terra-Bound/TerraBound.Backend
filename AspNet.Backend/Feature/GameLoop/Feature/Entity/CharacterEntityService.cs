using System.Numerics;
using Arch.Core;
using AspNet.Backend.Feature.Character;
using AspNet.Backend.Feature.GameLoop.Feature.Shared;
using LiteNetLib;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.GameLoop.Feature.Entity;

/// <summary>
/// The <see cref="CharacterEntityService"/> class
/// is a class responsible for creating, destroying and managing character entities.
/// </summary>
/// <param name="logger">The <see cref="Logger{T}"/>.</param>
/// <param name="world">The <see cref="World"/>.</param>
/// <param name="entityService">The <see cref="CharacterEntityService"/>.</param>
/// <param name="userIdToEntityMapper">The <see cref="UserIdToEntityMapper"/>.</param>
public class CharacterEntityService(ILogger<CharacterEntityService> logger, World world, EntityService entityService, UserIdToEntityMapper userIdToEntityMapper)
{
    /// <summary>
    /// Creates a default player/character <see cref="Entity"/>.
    /// </summary>
    /// <param name="world">The <see cref="World"/>.</param>
    /// <returns>The <see cref="Entity"/>.</returns>
    public static Arch.Core.Entity CreateTemplate(World world)
    {
        return world.Create(
            new Identity(-1, "char:1"),
            new TerraBound.Core.Components.Character(), 
            new NetworkedTransform(Vector2.Zero), 
            new Velocity(Vector2.Zero),
            new Movement(Vector2.Zero, 50f),
            new ChunkLoader(1),
            new Toggle<DirtyTransform>(false)
        );
    }
    
    /// <summary>
    /// Clones and initializes a player/character <see cref="Entity"/>.
    /// </summary>
    /// <param name="type">Its type.</param>
    /// <param name="peer">Its <see cref="NetPeer"/>.</param>
    /// <param name="characterDto">Its <see cref="CharacterModel"/>.</param>
    /// <returns></returns>
    public Arch.Core.Entity Create(string type, NetPeer peer, CharacterDto characterDto)
    {
        var entity = Prototyper.Clone(world, type);
        world.Set(entity, new Identity(characterDto.Id, characterDto.Type));
        world.Set(entity, new TerraBound.Core.Components.Character(characterDto.UserId, peer, characterDto.Username));
        world.Set(entity, new NetworkedTransform(characterDto.Transform.Position));
        
        entityService.AddEntity(characterDto.Id, entity);
        userIdToEntityMapper.Add(characterDto.UserId, entity);
        
        logger.LogInformation("Created {Entity} from {Character} with {Identity}", entity, characterDto, characterDto.Id);
        return entity;
    }

    /// <summary>
    /// Disposes an <see cref="Entity"/>s resources by removing it from the <see cref="EntityService"/> and other mappers.
    /// <remarks>Does not destroy it.</remarks>
    /// </summary>
    /// <param name="entity">The instance.</param>
    public void Dispose(Arch.Core.Entity entity)
    {
        ref var entityData = ref world.GetEntityData(entity);
        ref var identity = ref entityData.Get<Identity>();
        ref var character = ref entityData.Get<TerraBound.Core.Components.Character>();
        
        entityService.RemoveEntity(identity.Id);
        userIdToEntityMapper.Remove(character.GUID);
        
        logger.LogInformation("Disposed {Entity} of {Character} with {Identity}", entity, character, identity);
    }
}