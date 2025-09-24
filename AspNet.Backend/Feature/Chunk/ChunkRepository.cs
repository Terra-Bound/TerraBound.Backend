using System.Linq.Expressions;
using TerraBound.Core.Geo;

namespace AspNet.Backend.Feature.Chunk;

using Chunk = ChunkModel;

/// <summary>
/// The <see cref="ChunkRepository"/> class
/// provides repository-level utilities and operations related to <see cref="ChunkModel"/> entities.
/// </summary>
public class ChunkRepository
{
    /// <summary>
    /// Builds a predicate expression for filtering chunks based on their coordinates.
    /// The predicate matches chunks whose X and Y coordinates align with the provided grid positions.
    /// </summary>
    /// <param name="grids">An array of grid coordinates to match against chunk coordinates.</param>
    /// <returns>
    /// An expression representing the predicate logic for matching chunks based on the input grids.
    /// If no grids are provided, the expression always evaluates to false.
    /// </returns>
    public static Expression<Func<Chunk, bool>> BuildChunkCoordsPredicate(ReadOnlySpan<Grid> grids)
    {
        var param = Expression.Parameter(typeof(Chunk), "c");

        Expression? combined = null;

        var propX = Expression.Property(param, nameof(Chunk.X));
        var propY = Expression.Property(param, nameof(Chunk.Y));

        foreach (var g in grids)
        {
            // (c.X == g.X)
            var eqX = Expression.Equal(propX, Expression.Constant(g.X));
            // (c.Y == g.Y)
            var eqY = Expression.Equal(propY, Expression.Constant(g.Y));
            // (c.X == g.X && c.Y == g.Y)
            var and = Expression.AndAlso(eqX, eqY);

            combined = combined == null ? and : Expression.OrElse(combined, and);
        }

        // When combined null (no grids), return false
        combined ??= Expression.Constant(false);
        return Expression.Lambda<Func<Chunk, bool>>(combined, param);
    }
}