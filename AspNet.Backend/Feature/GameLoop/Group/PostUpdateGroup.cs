using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using AspNet.Backend.Feature.GameLoop.Feature.Entity;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.GameLoop.Group;

/// <summary>
///     A system group which cleans up entities and data. 
/// </summary>
public sealed class PostUpdateGroup(
    ILogger<GameLoopService> logger,
    ILogger<DatabaseGroup> dbLogger,
    IServiceProvider serviceProvider,
    World world,
    CharacterEntityService characterEntityService,
    ChunkEntityService chunkEntityService
) : Group<float>(
    "PostUpdateGroup",
    new DatabaseGroup(dbLogger, serviceProvider, world),
    new DisposeSystem(world, characterEntityService, chunkEntityService)    // Deinitialize entities
);

/// <summary>
/// The <see cref="DisposeSystem"/>
/// is a system destroying and cleaning up entities. 
/// </summary>
/// <param name="world">The <see cref="World"/>.</param>
public sealed partial class DisposeSystem(
    World world,
    CharacterEntityService characterEntityService,
    ChunkEntityService chunkEntityService
) : BaseSystem<World, float>(world)
{
    private readonly QueryDescription _destroyQuery = new QueryDescription().WithAll<Destroy>();

    /// <summary>
    /// Disposes a <see cref="Character"/> entity and releases its resources. 
    /// </summary>
    /// <param name="entity">Its instance.</param>
    [Query]
    [All<TerraBound.Core.Components.Character, Destroy>]
    private void DisposeCharacter(Entity entity)
    {
        characterEntityService.Dispose(entity);
    }
    
    /// <summary>
    /// Disposes a <see cref="TerraBound.Core.Components.Chunk"/> entity and releases its resources. 
    /// </summary>
    /// <param name="entity">Its instance.</param>
    [Query]
    [All<TerraBound.Core.Components.Chunk, Destroy>]
    private void DisposeChunk(Entity entity)
    {
        chunkEntityService.Dispose(entity);
    }
    
    /// <summary>
    /// Destroys all entities marked with <see cref="Destroy"/>.
    /// </summary>
    /// <param name="t"></param>
    public override void AfterUpdate(in float t)
    {
        base.AfterUpdate(in t);
        World.Destroy(_destroyQuery);
    }
}