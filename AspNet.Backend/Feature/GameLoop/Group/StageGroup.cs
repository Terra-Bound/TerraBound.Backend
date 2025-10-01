using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions.Dangerous;
using Arch.System;
using Arch.System.SourceGenerator;
using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Character;
using AspNet.Backend.Feature.Chunk;
using AspNet.Backend.Feature.GameLoop.Feature.Entity;
using AspNet.Backend.Feature.GameLoop.Feature.Networking;
using AspNet.Backend.Feature.GameLoop.Feature.Shared;
using AspNet.Backend.Feature.Shared;
using TerraBound.Core.Components;
using TerraBound.Core.Geo;
using TerraBound.Core.Network;

namespace AspNet.Backend.Feature.GameLoop.Group;

/// <summary>
///     A system group which processes commands, initializes entities and applies data. 
/// </summary>
public sealed class StageGroup(
    ILogger<GameLoopService> logger,
    IServiceProvider provider,
    World world,
    EntityMapper mapper,
    EntityService entityService,
    CharacterEntityService characterEntityService,
    ChunkEntityService chunkEntityService,
    NetworkCommandService commandService
) : Group<float>(
    "StageGroup",
    /*new ChunkSystem(logger, provider, world, chunkEntityService), */                                               // Load/Unload chunks
    new InitialisationSystem(logger, provider, world, entityService, characterEntityService, commandService),    // Initialize entities
    new CommandGroup(logger, world, mapper)                                                                      // Apply commands to entities and game
);

/// <summary>
/// The <see cref="InitialisationSystem"/>
/// is a system listening to certain events for initialising entities or preparing data.
/// </summary>
/// <param name="world">The <see cref="World"/>.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
public sealed partial class InitialisationSystem(
    ILogger<GameLoopService> logger,
    IServiceProvider serviceProvider,
    World world, 
    EntityService entityService,
    CharacterEntityService characterEntityService,
    NetworkCommandService commandService
) : BaseSystem<World, float>(world)
{
    private Scoped<UserService> UserService { get; set; } = new(serviceProvider);

    /// <summary>
    /// Initializes a character from a <see cref="OnConnectionEstablished"/>.
    /// </summary>
    /// <param name="request">The <see cref="OnConnectionEstablished"/> request.</param>
    [Query]
    private void OnLoginSpawnCharacter(in OnConnectionEstablished request)
    {
        // Get user
        var user = UserService.Value.GetUserByIdAsync(request.UUID).GetAwaiter().GetResult();
        if (user == null)
        {
            logger.LogError(Error.UserNotFound);
            return;
        }

        var characterDto = user.Character!.ToDto();
        
        // Spawn user in ECS & on client
        var entity = characterEntityService.Create(characterDto.Type, request.Peer, characterDto);
        commandService.SendSpawnAndCenterOnMapCommand(request.Peer, characterDto.Id, characterDto.Type, characterDto.Transform.Position);
        
        logger.LogDebug("Spawned Character on login of User {GUID} as {Entity}", characterDto.Id, entity);
    }
    
    /// <summary>
    /// Updates a character from a <see cref="OnReconnected"/> and spawns it back on the client.
    /// </summary>
    /// <param name="request">The <see cref="OnReconnected"/> request.</param>
    [Query]
    private void OnReconnectSpawnCharacter(in OnReconnected request)
    {
        // Get entity
        var entity = DangerousEntityExtensions.CreateEntityStruct(request.EntityId, World.Id, 1);

        // Update peer
        ref var entityData = ref world.GetEntityData(entity);
        ref var identity = ref entityData.Get<Identity>();
        ref var character = ref entityData.Get<TerraBound.Core.Components.Character>();
        ref var transform = ref entityData.Get<NetworkedTransform>();
        character.Peer = request.Peer;
        entityService.RemoveDestroyAfter(entity);  // Remove destroy/after or keep alive component
        
        // Spawn user on client 
        commandService.SendSpawnAndCenterOnMapCommand(request.Peer, identity.Id, identity.Type, transform.Position);
        
        // Map userId to entity
        logger.LogDebug("Spawned in Character on reconnect of User {GUID} as {Entity}", character.GUID, entity);
    }
    
    /// <summary>
    /// Marks a character for destruction once a <see cref="OnConnectionLost"/> was received.
    /// </summary>
    /// <param name="request">The <see cref="OnConnectionLost"/> request.</param>
    [Query]
    private void OnDisconnectDestroyCharacterAfter(in OnConnectionLost request)
    {
        // Get entity
        var entity = DangerousEntityExtensions.CreateEntityStruct(request.EntityId, World.Id, 1);
        
        ref var character = ref world.Get<TerraBound.Core.Components.Character>(entity);
        entityService.AddDestroyAfter(entity);
        
        // Map userId to entity
        logger.LogDebug("Marked Character of User with {GUID} as {Entity} for destruction after 10 minutes", character.GUID, entity);
    }
    
    public override void AfterUpdate(in float t)
    {
        base.AfterUpdate(in t);
        UserService.Dispose();
    }
}

/// <summary>
/// The <see cref="CommandGroup"/>
/// is a system listening to certain events or networking commands to apply them to the ecs. 
/// </summary>
/// <param name="world">The <see cref="World"/>.</param>
/// <param name="entityMapper">The <see cref="EntityMapper"/>.</param>
public sealed partial class CommandGroup(
    ILogger<GameLoopService> logger,
    World world, 
    EntityMapper entityMapper
) : BaseSystem<World, float>(world) 
{
    /// <summary>
    /// Moves a character from a <see cref="DoubleClickCommand"/> request.
    /// </summary>
    /// <param name="command"></param>
    [Query]
    private void OnDoubleClickMoveCharacter(in DoubleClickCommand command)
    {
        var entity = entityMapper[command.Id];
        ref var movement = ref world.Get<Movement>(entity);
        ref var velocity = ref world.Get<Velocity>(entity);
        movement.Target = command.Position;
        velocity.Vel = Vector2.Zero;  // Reset velocity to stop moving. 
        
        logger.LogDebug("Double Click received for {Entity} to move to {MovementTarget}", entity, movement.Target);
    }
}

/// <summary>
/// The <see cref="ChunkSystem"/>
/// is a system loading and deloading chunks. 
/// </summary>
/// <param name="world">The <see cref="World"/>.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
public sealed partial class ChunkSystem(
    ILogger<GameLoopService> logger,
    IServiceProvider serviceProvider,
    World world, 
    ChunkEntityService chunkEntityService
) : BaseSystem<World, float>(world)
{
    private Scoped<ChunkService> ChunkService { get; set; } = new(serviceProvider);

    // TODO: Make this use commandbuffer, it adds components during query... thats forbidden
    /// <summary>
    /// Ensures the specified chunk remains active or unloads it based on its state.
    /// </summary>
    /// <param name="entity">The entity associated with the chunk.</param>
    /// <param name="chunkComponent">The chunk component to be evaluated and managed.</param>
    [Query, All<TerraBound.Core.Components.Chunk>, None<Destroy>]
    private void KeepChunkAliveOrUnload(Entity entity, ref TerraBound.Core.Components.Chunk chunkComponent)
    {
        chunkEntityService.KeepChunkAliveOrUnload(entity, chunkComponent);
    }

    // TODO: Runs one loading query for each chunkloader, can be optimized by batching all requests 
    /// <summary>
    /// Loads chunks existing around the chunkloaders.
    /// </summary>
    /// <param name="transform">The <see cref="NetworkedTransform"/> of the entity containing its position and current chunk grid.</param>
    [Query, All<ChunkLoader>, None<Destroy>]
    private void LoadChunks(ref NetworkedTransform transform)
    {
        var previousGrid = transform.ChunkGrid;
        var grid = ChunkEntityService.CalculateChunkGridFor(transform.Position.X, transform.Position.Y);
        
        // Only trigger once chunkloader moves
        if(previousGrid != grid)
        {
            var chunkService = ChunkService.Value;
            chunkEntityService.LoadChunksAroundGrid(chunkService, grid, 1);
        }
    }
    
    /// <summary>
    /// Creates chunks non existing around the chunkloaders.
    /// </summary>
    /// <param name="transform">The <see cref="NetworkedTransform"/> of the entity containing its position and current chunk grid.</param>
    [Query, All<ChunkLoader>, None<Destroy>]
    private void CreateChunks(ref NetworkedTransform transform)
    {
        var previousGrid = transform.ChunkGrid;
        var grid = ChunkEntityService.CalculateChunkGridFor(transform.Position.X, transform.Position.Y);
        
        // Only trigger once chunkloader moves
        if(previousGrid != grid)
        {
            chunkEntityService.CreateChunksAroundGrid(grid, 1);
        }
    }
    
    /// <summary>
    /// Assigns a chunk to an entity based on its current position and updates its grid location.
    /// </summary>
    /// <param name="entity">The <see cref="Entity"/> to be assigned to a chunk.</param>
    /// <param name="identity">The <see cref="Identity"/> of the entity containing unique identification.</param>
    /// <param name="transform">The <see cref="NetworkedTransform"/> of the entity containing its position and current chunk grid.</param>
    [Query, None<Destroy>]
    private void AssignChunk(Entity entity, in Identity identity, ref NetworkedTransform transform)
    {
        // Track previous grid
        var previousGrid = transform.ChunkGrid;
        
        // Calculate chunk grid
        var grid = ChunkEntityService.CalculateChunkGridFor(transform.Position.X, transform.Position.Y);

        // Assign to new grid on change
        chunkEntityService.SwitchChunks(entity, previousGrid, grid);

        // Update chunkloaders and update chunks
        ref var chunkLoader = ref world.TryGetRef<ChunkLoader>(entity, out var isChunkLoader);
        if (isChunkLoader)
        {
            chunkEntityService.SwitchChunksForChunkLoader(entity, ref chunkLoader, grid, previousGrid);
        }
        
        //logger.LogDebug("Calculated chunk {X}/{Y} for {GUID} as {Entity}", grid.X, grid.Y, identity.Id, entity);
    }

    
    public override void AfterUpdate(in float t)
    {
        base.AfterUpdate(in t);
        ChunkService.Dispose();
    }
}
