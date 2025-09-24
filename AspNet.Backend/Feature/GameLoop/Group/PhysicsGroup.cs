using System.Numerics;
using Arch.Core;
using Arch.System;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.GameLoop.Group;

/// <summary>
/// The <see cref="MovementSystem"/> class
/// is a system processing the movement and rotation of entities. 
/// </summary>
/// <param name="world"></param>
public sealed partial class MovementSystem(ILogger<GameLoopService> logger, World world) : BaseSystem<World, float>(world)
{
    [Query]
    private void MoveTo(ref NetworkedTransform transform, in Movement movement, ref Velocity velocity)
    {
        // If target is zero ignore otherwise entities might move all the way to 0;0 forever...
        if (movement.Target is { X: 0, Y: 0 }) return;

        // Calculate target vector 
        var toTarget = movement.Target - transform.Position;
        const float eps = 1e-6f;
        var distSq = toTarget.LengthSquared();

        // Prevent nana by clipping velocity
        if (distSq <= eps * eps || (movement.Target.X == 0f && movement.Target.Y == 0f))
        {
            velocity.Vel = Vector2.Zero;
            return;
        }

        // Normalize and apply 
        var distance = MathF.Sqrt(distSq);
        var direction = toTarget / distance;
        velocity.Vel = direction * movement.Speed;
    }
    
    [Query]
    private void PreventOvershooting([Data] in float deltaTime, ref NetworkedTransform transform, in Movement movement, ref Velocity velocity)
    {
        // If target is zero ignore otherwise entities might move all the way to 0;0 forever...
        if (movement.Target is { X: 0, Y: 0 }) return;

        var toTarget = movement.Target - transform.Position;
        var distance = toTarget.Length();
        var stepSize = velocity.Vel.Length() * deltaTime;

        // Prevent overshooting by stopping movement when arrived
        if (!(stepSize >= distance)) return;
        transform.Position = movement.Target;
        velocity.Vel = Vector2.Zero;
    }
    
    [Query]
    private void Move([Data] in float deltaTime, Arch.Core.Entity entity, ref NetworkedTransform transform, in Velocity velocity)
    {
        // Calculate position
        transform.Position += velocity.Vel * deltaTime;
        
        // Mark as dirty 
        var isMoving = velocity.Vel.X != 0f || velocity.Vel.Y != 0f;
        ref var dirtyTransform = ref World.TryGetRef<Toggle<DirtyTransform>>(entity, out var hasDirtyTransform);
        if (hasDirtyTransform)
        {
            dirtyTransform.Enabled = isMoving;
        }
    }
}