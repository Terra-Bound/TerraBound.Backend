using Arch.Buffer;
using Arch.Core;
using Arch.System;

namespace AspNet.Backend.Feature.Background.Systems;

/// <summary>
///     A system which contains a <see cref="CommandBuffer" /> which acts as a buffer for entity modifications.
///     It will play them back during the systems update loop.
/// </summary>
public class EntityCommandBufferSystem(World world) : BaseSystem<World, float>(world)
{
    /// <summary>
    ///     The command buffer used to play back recorded entity changes.
    /// </summary>
    public CommandBuffer EntityCommandBuffer { get; } = new();

    public override void Update(in float t)
    {
        base.Update(in t);
        
        // Execute the buffered entity commands 
        if (EntityCommandBuffer.Size <= 0) return;
        EntityCommandBuffer.Playback(World);
    }
}