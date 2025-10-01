using Arch.Core;
using AspNet.Backend.Feature.GameLoop.Feature.Entity;
using AspNet.Backend.Feature.GameLoop.Feature.Networking;
using Core.Systems;
using Arch.System;
using System.Numerics;
using Arch.Core;
using Arch.System;
using LiteNetLib;
using TerraBound.Core.Components;
using TerraBound.Core.Network;

namespace AspNet.Backend.Feature.GameLoop.Group;


/// <summary>
///     A system group which runs the main game simulation.
/// </summary>
public sealed class UpdateGroup(
    ILogger<GameLoopService> logger,
    World world,
    ServerNetworkService networkService
) : Group<float>(
    "UpdateGroup",
    new ReactiveSystem(world),
    new MovementSystem(logger, world),
    new NetworkSystem(world, networkService)
);


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

/// <summary>
/// The <see cref="NetworkSystem"/>
/// is a system synchronizing the state with the client by sending packages. 
/// </summary>
/// <param name="world"></param>
/// <param name="serverNetworkService"></param>
public sealed partial class NetworkSystem(World world, ServerNetworkService serverNetworkService) : BaseSystem<World, float>(world)
{
    // [Query]
    // private void SendCharacterData(in Identity identity, in TerraBound.Core.Components.Character character, in NetworkedTransform transform, in Toggle<DirtyTransform> toggle)
    // {
    //     if (!toggle.Enabled) return;
    //
    //     var entityCommand = new MoveCommand { Id = identity.Id, Position = transform.Position };
    //     serverNetworkService.Send(character.Peer, ref entityCommand, DeliveryMethod.Sequenced);
    // }
    
    [Query]
    private void SendTransform(in Identity identity, in TerraBound.Core.Components.Character character, in NetworkedTransform transform, in Toggle<DirtyTransform> toggle)
    {
        if (!toggle.Enabled) return;

        var entityCommand = new MoveCommand { Id = identity.Id, Position = transform.Position };
        serverNetworkService.Send(character.Peer, ref entityCommand, DeliveryMethod.Sequenced);
    }
}