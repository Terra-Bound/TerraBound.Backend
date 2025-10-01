using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using AspNet.Backend.Feature.Character;
using AspNet.Backend.Feature.Chunk;
using AspNet.Backend.Feature.Shared;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.GameLoop.Group;

/// <summary>
/// The <see cref="DatabaseGroup"/> is a class
/// that represents a group of systems which synchronize/persist entities in the database. 
/// </summary>
/// <param name="logger"></param>
/// <param name="world"></param>
/// <param name="provider"></param>
public sealed class DatabaseGroup(
    ILogger<DatabaseGroup> logger,
    IServiceProvider provider,
    World world
) : Group<float>(
    "DatabaseGroup",
    new SaveOnCreatedDatabaseSystem(logger, world, provider),
    new SaveOnDestroyDatabaseSystem(logger, world, provider),
    new IntervalGroup(60.0f, new IntervalDatabaseSystem(logger, world, provider))
);

/// <summary>
/// The <see cref="SaveOnCreatedDatabaseSystem"/> is a system that handles
/// saving newly created instances of entities in bulk into the database.
/// </summary>
/// <param name="logger">The logger used for logging information.</param>
/// <param name="world">The world containing the entities and systems.</param>
/// <param name="serviceProvider">Provides application services required by the system.</param>
public sealed partial class SaveOnCreatedDatabaseSystem(
    ILogger<DatabaseGroup> logger,
    World world,
    IServiceProvider serviceProvider) : BaseSystem<World, float>(world)
{
    private Scoped<ChunkService> ChunkService { get; set; } = new(serviceProvider);

    [Query]
    [All<Created>, None<Destroy>]
    private void SaveChunksOnCreated(in Identity identity, in TerraBound.Core.Components.Chunk chunkComponent)
    {
        // Add to service
        var model = ChunkMapper.ToDto(identity, chunkComponent);   
        ChunkService.Value.CreateInBulkAsync(model);
        logger.LogDebug("Saved {Chunk} with {Identity}", chunkComponent, identity);
    }
    
    public override void AfterUpdate(in float t)
    {
        base.AfterUpdate(in t);

        // Skip if ChunkService was not used, prevents memory allocation of a new service.
        if (!ChunkService.HasValue)
        {
            return;
        }
        
        ChunkService.Value.SaveChangesInBulkAsync().GetAwaiter().GetResult();
        ChunkService.Dispose();
        logger.LogInformation("Saved created instances");
    }
}

/// <summary>
/// The <see cref="SaveOnDestroyDatabaseSystem"/>
/// is a system saving or deleting entities based on actions.
/// </summary>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="world">The <see cref="World"/>.</param>
/// <param name="serviceProvider">The <see cref="ServiceProvider"/>.</param>
public sealed partial class SaveOnDestroyDatabaseSystem(ILogger<DatabaseGroup> logger, World world, IServiceProvider serviceProvider) : BaseSystem<World, float>(world)
{
    private Scoped<CharacterService> CharacterService { get; set; } = new(serviceProvider);

    [Query]
    [All<Destroy>]
    private void SaveCharacterOnDestroy(in Identity identity, in TerraBound.Core.Components.Character character, in NetworkedTransform transform)
    {
        // Add to service
        var model = CharacterMapper.ToDto(identity, character, transform);   
        CharacterService.Value.UpdateInBulkAsync(model);
        logger.LogDebug("Saved {Character} with {Identity}", character, identity);
    }
    
    public override void AfterUpdate(in float t)
    {
        base.AfterUpdate(in t);
        
        // Skip if ChunkService was not used, prevents memory allocation of a new service.
        if (!CharacterService.HasValue)
        {
            return;
        }
        
        CharacterService.Value.SaveChangesInBulkAsync().GetAwaiter().GetResult();
        CharacterService.Dispose();
        logger.LogInformation("Saved instances before destruction");
    }
}

// TODO: Mark entities with a flag/component to indicate they need an db update to reduce pressure.
/// <summary>
/// The <see cref="IntervalDatabaseSystem"/>
/// is a system synchronizing the database with the entities on a regular interval. 
/// </summary>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="world">The <see cref="World"/>.</param>
/// <param name="serviceProvider">The <see cref="ServiceProvider"/>.</param>
public sealed partial class IntervalDatabaseSystem(ILogger<DatabaseGroup> logger, World world, IServiceProvider serviceProvider) : BaseSystem<World, float>(world)
{
    private Scoped<CharacterService> CharacterService { get; set; } = new(serviceProvider);

    [Query]
    [None<Destroy>]
    private void SaveCharacter(in Identity identity, in TerraBound.Core.Components.Character character, in NetworkedTransform transform)
    {
        // Add to service
        var model = CharacterMapper.ToDto(identity, character, transform);  
        CharacterService.Value.UpdateInBulkAsync(model);
        logger.LogDebug("Saved {Character} with {Identity}", character, identity);
    }
    
    public override void AfterUpdate(in float t)
    {
        base.AfterUpdate(in t);
        
        // Skip if ChunkService was not used, prevents memory allocation of a new service.
        if (!CharacterService.HasValue)
        {
            return;
        }
        
        CharacterService.Value.SaveChangesInBulkAsync().GetAwaiter().GetResult();
        CharacterService.Dispose();
        logger.LogInformation("Saved updated instances");
    }
}