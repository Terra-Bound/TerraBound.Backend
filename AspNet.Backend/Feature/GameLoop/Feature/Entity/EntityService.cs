using Arch.Buffer;
using Arch.Core;
using AspNet.Backend.Feature.GameLoop.Feature.Shared;
using AspNet.Backend.Feature.Shared;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.GameLoop.Feature.Entity;

/// <summary>
/// The <see cref="EntityService"/> class
/// acts as a class to create, destroy and modify entities. And to manage their lifecycle. 
/// </summary>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="world">The <see cref="World"/>.</param>
/// <param name="mapper">The <see cref="EntityMapper"/>.</param>
public class EntityService(
    ILogger<GameLoopService> logger,
    World world, 
    CommandBuffer entityCommandBuffer,
    EntityMapper mapper
)
{
    /// <summary>
    /// Provides access to the <see cref="CommandBuffer"/> instance associated with the service,
    /// allowing the execution of buffered entity commands in the context of the ECS (Entity Component System).
    /// </summary>
    /// <remarks>
    /// The <c>EntityCommandBuffer</c> property is primarily used to queue operations such as creating, destroying, and modifying entities.
    /// These operations are deferred until the buffer is executed, improving performance and ensuring thread safety. This particular buffer is executed before the <see cref="UpdateGroup"/>. 
    /// </remarks>
    public CommandBuffer EntityCommandBuffer
    {
        get => entityCommandBuffer;
    }

    /// <summary>
    /// Adds an <see cref="Entity"/>.
    /// </summary>
    /// <param name="id">Its unique id.</param>
    /// <param name="entity">Its instance.</param>
    /// <returns>True if successfully.</returns>
    public bool AddEntity(int id, Arch.Core.Entity entity)
    {
        return mapper.TryAdd(id, entity);
    }

    /// <summary>
    /// Adds an <see cref="Entity"/> using the given <see cref="Identity"/>.
    /// <remarks>Assign a new unique id if the passed is below 0.</remarks>
    /// </summary>
    /// <param name="identity">The <see cref="Identity"/> reference which provides a unique identifier for the entity.</param>
    /// <param name="entity">The instance of the <see cref="Arch.Core.Entity"/> to be added.</param>
    /// <returns>True if the addition was successful, false otherwise.</returns>
    public bool AddEntity(ref Identity identity, Arch.Core.Entity entity)
    {
        if (identity.Id <= 0)
        {
            identity.Id = (int)RandomExtensions.GetUniqueInt();
        }
        return mapper.TryAdd(identity.Id, entity);
    }
    
    /// <summary>
    /// Returns true if an <see cref="Entity"/> exists by its unique id.
    /// </summary>
    /// <param name="id">Its id.</param>
    /// <param name="entity">Its instance.</param>
    /// <returns>True if it exists, false if it does not.</returns>
    public bool TryGetEntity(int id, out Arch.Core.Entity entity)
    {
        if (mapper.TryGetValue(id, out entity)) return true;
        logger.LogError(Error.EntityNotFound);
        return false;
    }

    /// <summary>
    /// Removes an <see cref="Entity"/>.
    /// </summary>
    /// <param name="id">Its id.</param>
    /// <returns>True if successfully.</returns>
    public bool RemoveEntity(int id)
    {
        return mapper.Remove(id);
    }

    /// <summary>
    /// Marks the entity with an <see cref="DestroyAfter"/> component that destructs the entity once the keep alive is over.
    /// </summary>
    /// <param name="entity">The passed <see cref="Entity"/></param>
    public void AddDestroyAfter(Arch.Core.Entity entity)
    {
        entityCommandBuffer.Set(entity, new DestroyAfter{ Milliseconds = Constants.KeepAliveInMs });  // Destroy after 10 minutes
    }
    
    /// <summary>
    /// Removes the <see cref="DestroyAfter"/> component from an entity.
    /// </summary>
    /// <param name="entity">The passed <see cref="Entity"/></param>
    public void RemoveDestroyAfter(Arch.Core.Entity entity)
    {
        entityCommandBuffer.Remove<DestroyAfter>(entity);
    }
}