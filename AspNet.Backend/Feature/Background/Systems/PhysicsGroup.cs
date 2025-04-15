using System.Numerics;
using Arch.Core;
using Arch.System;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.Background.Systems;

/// <summary>
/// The <see cref="MovementSystem"/> class
/// is a system processing the movement and rotation of entities. 
/// </summary>
/// <param name="world"></param>
public sealed partial class MovementSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    private void MoveTo(ref NetworkedTransform transform, in Movement movement, ref Velocity velocity)
    {
        // If target is zero ignore otherwise entities might move all the way to 0;0 forever...
        if (movement.Target is { X: 0, Y: 0 }) return;

        var direction = movement.Target - transform.Position; 
        direction = Vector2.Normalize(direction); // Normalize for equal and smooth movement
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
    private void Move([Data] in float deltaTime, ref NetworkedTransform transform, in Velocity velocity)
    {
        transform.Position += velocity.Vel * deltaTime;
    }
}