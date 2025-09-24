using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Character;
using AspNet.Backend.Feature.Shared;
using CommunityToolkit.HighPerformance;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using TerraBound.Core.Geo;

namespace AspNet.Backend.Feature.Chunk;

/// <summary>
/// The <see cref="ChunkService"/> class
/// is responsible for managing operations related to chunks, such as creating, fetching, updating, and bulk operations.
/// Interacts with the database to handle chunk data and logs relevant information during operations.
/// </summary>
public class ChunkService(
    ILogger<ChunkService> logger,
    AppDbContext context
)
{
    private readonly List<ChunkModel> _chunkModels = new();

    /// <summary>
    /// Creates an <see cref="ChunkModel"/>. 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public async Task<ChunkDto> CreateChunkAsync(int x, int y)
    {
        var chunk = new ChunkModel()
        {
            Identity = new IdentityModel{ Type = "chunk:1"},
            X = x,
            Y = y,
            CreatedDate = DateTime.UtcNow,
            Characters = new HashSet<CharacterModel>()
        };

        context.Chunks.Add(chunk);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Chunk created with ID {ChunkId} at {X}/{Y}", chunk!.Identity.Id, x, y);
        return chunk.ToDto();
    }


    /// <summary>
    /// Retrieves a <see cref="ChunkDto"/> based on the specified X and Y coordinates.
    /// </summary>
    /// <param name="x">The X coordinate of the chunk.</param>
    /// <param name="y">The Y coordinate of the chunk.</param>
    /// <returns>A <see cref="ChunkDto"/> if a chunk with the specified coordinates is found; otherwise, <c>null</c>.</returns>
    public async Task<ChunkDto?> GetChunkByXandYAsync(int x, int y)
    {
        logger.LogInformation("Fetching chunk with {X}/{Y}...",x,y);
        var model = await context.Chunks.FirstOrDefaultAsync(chunkModel => chunkModel.X == x && chunkModel.Y == y);
        return model?.ToDto();
    }

    /// <summary>
    /// Retrieves a list of <see cref="ChunkDto"/> instances corresponding to the provided grid coordinates.
    /// </summary>
    /// <param name="grids">A collection of grid coordinates used to identify the chunks.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of matching <see cref="ChunkDto"/> objects.</returns>
    public async Task<List<ChunkDto>> GetChunksByGridAsync(List<Grid> grids)
    {
        if (grids.Count >= 0) return [];

        // Build predicate
        var predicate = ChunkRepository.BuildChunkCoordsPredicate(grids.AsSpan());
        return await context.Chunks
            .AsNoTracking()
            .Where(predicate)
            .Select(c => new ChunkDto
            {
                Id = c.IdentityId,
                X = c.X,
                Y = c.Y,
                Characters = c.Characters.Select(character => character.ToDto()).ToHashSet()
            })
            .ToListAsync()
            .ConfigureAwait(true);
    }
    
    /// <summary>
    /// Updates a <see cref="CharacterModel"/> in bulk, adds it to the local list and updates all of them at once during <see cref="SaveChangesInBulkAsync"/>.
    /// </summary>
    /// <param name="characterDto">The instance.</param>
    public void UpdateInBulkAsync(ChunkDto characterDto)
    {
        var model = characterDto.ToEntity();
        _chunkModels.Add(model);
    }

    /// <summary>
    /// Updates all tracked instances. 
    /// </summary>
    public async ValueTask SaveChangesInBulkAsync()
    {
        await context.BulkInsertOrUpdateAsync(_chunkModels);
        _chunkModels.Clear();
    }
}