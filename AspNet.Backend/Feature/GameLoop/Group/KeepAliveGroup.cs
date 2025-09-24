using Arch.Core;
using Arch.System;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.GameLoop.Group;

/// <summary>
///     A system group which cleans up entities and data. 
/// </summary>
public sealed class KeepAliveGroup(
    ILogger<GameLoopService> logger,
    World world
) : Group<float>(
    "KeepAliveGroup",
    new KeepAliveSystem(world)    // Deinitialize entities
);

/// <summary>
/// The <see cref="DisposeSystem"/>
/// is a system destroying and cleaning up entities. 
/// </summary>
/// <param name="world">The <see cref="World"/>.</param>
public sealed partial class KeepAliveSystem(
    World world
) : BaseSystem<World, float>(world)
{
    /// <summary>
    /// Calculates the remaining keepalive of an entity before marking it for destruction. 
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    /// <param name="entity">The entity.</param>
    /// <param name="keepAlive">Its remaining keepAlive.</param>
    [Query]
    private void CalculateRemainingKeepAlive([Data] float deltaTime, Arch.Core.Entity entity, ref DestroyAfter keepAlive)
    {
        var deltaMs = (int)(deltaTime * 1000f);
        keepAlive.Milliseconds -= deltaMs;

        // Mark for destroy
        if (keepAlive.Milliseconds <= 0)
        {
            World.Remove<DestroyAfter>(entity);
            World.Add<Destroy>(entity);
        }
    }
}