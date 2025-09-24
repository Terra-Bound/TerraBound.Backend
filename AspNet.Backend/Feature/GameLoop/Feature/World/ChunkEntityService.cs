using System.Runtime.InteropServices;
using Arch.Core;
using AspNet.Backend.Feature.Character;
using AspNet.Backend.Feature.Chunk;
using CommunityToolkit.HighPerformance;
using TerraBound.Core.Components;
using TerraBound.Core.Geo;
using ChunkMapper = AspNet.Backend.Feature.GameLoop.Feature.Shared.ChunkMapper;

namespace AspNet.Backend.Feature.GameLoop.Feature.Entity;

/// <summary>
/// The <see cref="ChunkEntityService"/> class
/// is a class responsible for creating, destroying and managing chunk entities.
/// </summary>
/// <param name="logger">The <see cref="Logger{T}"/>.</param>
/// <param name="world">The <see cref="World"/>.</param>
/// <param name="entityService">The <see cref="CharacterEntityService"/>.</param>
/// <param name="chunkMapper">The <see cref="ChunkMapper"/>.</param>
public class ChunkEntityService(ILogger<ChunkEntityService> logger, World world, EntityService entityService, ChunkMapper chunkMapper)
{
    /// <summary>
    /// Creates a default player/character <see cref="Entity"/>.
    /// </summary>
    /// <param name="world">The <see cref="World"/>.</param>
    /// <returns>The <see cref="Entity"/>.</returns>
    public static Arch.Core.Entity CreateTemplate(World world)
    {
        return world.Create(
            new Identity(-1, "chunk:1"),
            new TerraBound.Core.Components.Character()
        );
    }
    
    /// <summary>
    /// Clones and initializes a chunk <see cref="Entity"/>.
    /// </summary>
    /// <param name="type">Its type.</param>
    /// <param name="chunkDto">Its <see cref="ChunkDto"/>.</param>
    /// <returns></returns>
    public Arch.Core.Entity Create(string type, ChunkDto chunkDto)
    {
        var entity = Prototyper.Clone(world, type);
        world.Set(entity, new Identity(chunkDto.Id, chunkDto.Type));
        world.Set(entity, new TerraBound.Core.Components.Chunk(chunkDto.X, chunkDto.Y));
        
        entityService.AddEntity(chunkDto.Id, entity);
        chunkMapper.Add(new Grid(chunkDto.X, chunkDto.Y), entity);
        
        logger.LogInformation("Created {Entity}/{Chunk} with {Identity}", entity, chunkDto, chunkDto.Id);
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
        ref var chunk = ref entityData.Get<TerraBound.Core.Components.Chunk>();
        
        entityService.RemoveEntity(identity.Id);
        chunkMapper.Remove(chunk.Grid);
        
        logger.LogInformation("Disposed {Entity}/{Chunk} with {Identity}", entity, chunk, identity);
    }

    /// <summary>
    /// Calculates the grid position for a chunk based on the provided x and y coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate in the world space.</param>
    /// <param name="y">The y-coordinate in the world space.</param>
    /// <returns>A <see cref="TerraBound.Core.Geo.Grid"/> representing the chunk's grid position.</returns>
    public static Grid CalculateChunkGridFor(float x, float y)
    {
        return new Grid((int)(x / 1000), (int)(y / 1000));
    }
    
    /// <summary>
    /// Returns true if a chunk exists at the provided x and y coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate in the world space.</param>
    /// <param name="y">The y-coordinate in the world space.</param>
    /// <param name="chunkEntity">The chunk entity.</param>
    /// <returns>True if it exists, false if it does not.</returns>
    public bool TryGetChunk(int x, int y, out Arch.Core.Entity chunkEntity)
    {
        if (!chunkMapper.TryGetValue(new Grid(x, y), out var entity))
        {
            chunkEntity = Arch.Core.Entity.Null;
            logger.LogWarning("Chunk not found at {X}, {Y}", x, y);
            return false;
        }

        chunkEntity = entity;
        return true;
    }

    /// <summary>
    /// Retrieves the chunks within the specified radius around the given grid position.
    /// </summary>
    /// <param name="grid">The central grid position from which to calculate the surrounding chunks.</param>
    /// <param name="radius">The radius in grid units to search for chunks around the specified grid position.</param>
    /// <param name="chunks">The collection to store the retrieved chunks.</param>
    public void GetChunksAroundGrid(Grid grid, int radius, Span<Arch.Core.Entity> chunks)
    {
        var index = 0;
        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                // Check if it exists 
                var gridPosition = new Grid(grid.X + dx, grid.Y + dy);
                if (!TryGetChunk(gridPosition.X, gridPosition.Y, out var chunkEntity)) continue;
                
                chunks[index] = chunkEntity;
                index++;
            } 
        }
    }
    
    /// <summary>
    /// Identifies missing chunks around a specified grid within a given radius and adds them to a collection.
    /// </summary>
    /// <param name="grid">The central <see cref="Grid"/> from which to calculate missing chunks.</param>
    /// <param name="radius">The radius around the grid to search for missing chunks.</param>
    /// <param name="missingChunks">A collection to store the grids of missing chunks.</param>
    public void GetMissingChunksAroundGrid(Grid grid, int radius, Span<Grid> missingChunks)
    {
        var index = 0;
        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                // Check if it exists 
                var gridPosition = new Grid(grid.X + dx, grid.Y + dy);
                if (!TryGetChunk(gridPosition.X, gridPosition.Y, out _))
                {
                    missingChunks[index] = (gridPosition);
                    index++;
                }
            } 
        }
    }
    
    // TODO: Make this more efficient: First collect all missing chunks, then load and instantiate them in parallel.
    /// <summary>
    /// Ensures that chunks around the specified grid are loaded and initialized within the defined radius.
    /// </summary>
    /// <param name="chunkService">The <see cref="ChunkService"/> being used.</param>
    /// <param name="grid">The central <see cref="Grid"/> around which chunks are to be ensured.</param>
    /// <param name="radius">The radius defining the area around the grid where chunks are to be checked or loaded.</param>
    public void LoadChunksAroundGrid(ChunkService chunkService, Grid grid, int radius)
    {
        var missingChunks = new List<Grid>(radius * 9);
        GetMissingChunksAroundGrid(grid, radius, missingChunks.AsSpan());

        // Load missing chunks
        var chunkDtos = chunkService.GetChunksByGridAsync(missingChunks).GetAwaiter().GetResult();
        foreach (var chunkDto in chunkDtos)
        {
            Create(chunkDto.Type, chunkDto);
        }
    }
    
    // TODO: Make this more efficient: First collect all missing chunks, then load and instantiate them in parallel.
    /// <summary>
    /// Ensures that chunks around the specified grid are loaded and initialized within the defined radius.
    /// </summary>
    /// <param name="grid">The central <see cref="Grid"/> around which chunks are to be ensured.</param>
    /// <param name="radius">The radius defining the area around the grid where chunks are to be checked or loaded.</param>
    public void CreateChunksAroundGrid(Grid grid, int radius)
    {
        var missingChunks = new List<Grid>(radius * 9);
        GetMissingChunksAroundGrid(grid, radius, missingChunks.AsSpan());

        // Load missing chunks
        foreach (var chunkGrid in missingChunks)
        {
            Create("chunk:1", new ChunkDto
            {
                Id = -1,
                Type = "chunk:1",
                X = chunkGrid.X,
                Y = chunkGrid.Y,
                Characters = new HashSet<CharacterDto>(),
                CreatedDate = DateTime.Now
            });
        }
    }

    /// <summary>
    /// Adds an entity to the specified chunk.
    /// </summary>
    /// <param name="chunkEntity">The <see cref="Arch.Core.Entity"/> representing the chunk.</param>
    /// <param name="entity">The <see cref="Arch.Core.Entity"/> to add to the chunk.</param>
    public void EnterChunk(Arch.Core.Entity chunkEntity, Arch.Core.Entity entity)
    {
        ref var chunk = ref world.Get<TerraBound.Core.Components.Chunk>(chunkEntity);
        chunk.Entities.Add(entity);
    }
    
    /// <summary>
    /// Removes an entity from the specified chunk.
    /// </summary>
    /// <param name="chunkEntity">The <see cref="Arch.Core.Entity"/> representing the chunk.</param>
    /// <param name="entity">The <see cref="Arch.Core.Entity"/> to remove from the chunk.</param>
    public void ExitChunk(Arch.Core.Entity chunkEntity, Arch.Core.Entity entity)
    {
        ref var chunk = ref world.Get<TerraBound.Core.Components.Chunk>(chunkEntity);
        chunk.Entities.Remove(entity);
    }

    /// <summary>
    /// Switches the chunk the entity is currently in to the specified chunk.
    /// </summary>
    /// <param name="entity">The <see cref="Arch.Core.Entity"/> to switch chunks for.</param>
    /// <param name="currentGrid">The current <see cref="Grid"/> the entity is currently in.</param>
    /// <param name="newGrid">The new <see cref="Grid"/> the entity should be in.</param>
    /// <remarks>
    /// If the entity is already in the specified chunk, this method does nothing.
    /// If the entity is not in the specified chunk, it will be moved to the specified chunk.
    /// If the specified chunk does not exist, this method does nothing.
    /// </remarks>
    public void SwitchChunks(Arch.Core.Entity entity, Grid currentGrid, Grid newGrid)
    {
        // Assign to new grid on change
        if (currentGrid == newGrid) return;
        
        // Assign new chunkgrid
        ref var transform = ref world.Get<NetworkedTransform>(entity);
        transform.ChunkGrid = newGrid;
        
        // Remove from old grid (if exists, might not if it spawns first e.g.)
        if(TryGetChunk(currentGrid.X, currentGrid.Y, out var oldChunk)) ExitChunk(oldChunk, entity);
        
        // Make sure new chunks exist
        if (!TryGetChunk(newGrid.X, newGrid.Y, out var newChunk)) return;
        EnterChunk(newChunk, entity);
    }

    /// <summary>
    /// Updates the chunk loader's current and previous grids, transitioning the entity from the previous chunk state to the new chunk state.
    /// Modifies the chunks around the loader to reflect loading and unloading states.
    /// </summary>
    /// <param name="entity">The entity that represents the loader or subject performing the chunk state transitions.</param>
    /// <param name="chunkLoader">The chunk loader reference, containing information about the radius and associated grids.</param>
    /// <param name="currentGrid">The grid representing the current position of the loader.</param>
    /// <param name="previousGrid">The grid representing the loader's previous position.</param>
    public void SwitchChunksForChunkLoader(Arch.Core.Entity entity, ref ChunkLoader chunkLoader,
        Grid currentGrid, Grid previousGrid)
    {
        chunkLoader.CurrentGrid = currentGrid;
        chunkLoader.PreviousGrid = previousGrid;
        
        // Get chunks around grid
        Span<Arch.Core.Entity> oldChunksInRange = stackalloc Arch.Core.Entity[chunkLoader.Radius * 9];
        Span<Arch.Core.Entity> newChunksInRange = stackalloc Arch.Core.Entity[chunkLoader.Radius * 9];
        
        GetChunksAroundGrid(previousGrid, chunkLoader.Radius, oldChunksInRange);
        GetChunksAroundGrid(currentGrid, chunkLoader.Radius, newChunksInRange);

        // Remove from old chunks
        foreach (var chunkEntity in oldChunksInRange)
        {
            ref var chunk = ref world.Get<TerraBound.Core.Components.Chunk>(chunkEntity);
            chunk.LoadedBy.Remove(entity);
        }

        // Add to new chunks
        foreach (var chunkEntity in newChunksInRange)
        {
            ref var chunk = ref world.Get<TerraBound.Core.Components.Chunk>(chunkEntity);
            chunk.LoadedBy.Add(entity); 
        }
    }

    /// <summary>
    /// Updates the specified chunk's lifecycle by deciding whether to keep it active or mark it for unloading.
    /// </summary>
    /// <remarks>If the chunk is not loaded by any <see cref="ChunkLoader"/> it is considered inactive and is marked for destruction.</remarks>
    /// <param name="entity">The <see cref="Arch.Core.Entity"/> representing the chunk.</param>
    /// <param name="chunk">The <see cref="TerraBound.Core.Components.Chunk"/> data associated with the entity.</param>
    public void KeepChunkAliveOrUnload(Arch.Core.Entity entity, TerraBound.Core.Components.Chunk chunk)
    {
        var chunkMarkedForDestruction = world.Has<DestroyAfter>(entity);
        if(chunk.LoadedBy.Count > 0)
        {
            if (chunkMarkedForDestruction)
            { 
                entityService.RemoveDestroyAfter(entity);
            }
            return;
        }
        
        entityService.AddDestroyAfter(entity);
    }
}