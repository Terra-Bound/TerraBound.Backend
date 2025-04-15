using Arch.Core;
using Arch.System;
using AspNet.Backend.Feature.AppUser;
using LiteNetLib;
using TerraBound.Core.Components;
using TerraBound.Core.Network;

namespace AspNet.Backend.Feature.Background.Systems;

/// <summary>
/// The <see cref="InitialisationGroup"/>
/// is a system listening to certain events for initialising entities or preparing data.
/// </summary>
/// <param name="world">The <see cref="World"/>.</param>
/// <param name="userService">The <see cref="UserService"/>.</param>
/// <param name="serverNetworkService">The <see cref="ServerNetworkService"/>.</param>
/// <param name="entityMapper">The <see cref="EntityMapper"/>.</param>
public sealed partial class InitialisationGroup(
    World world, 
    UserService userService, 
    ServerNetworkService serverNetworkService, 
    EntityMapper entityMapper
) : BaseSystem<World, float>(world) 
{
    [Query]
    private void InitializeCharacter(in OnConnectionEstablished request)
    {
        // Get user
        var user = userService.GetUserByIdAsync(request.UUID).GetAwaiter().GetResult();
        if (user == null)
        {
            //logger.LogError(Error.UserNotFound);
            return;
        }
        var characterModel = user.CharacterModel;
        
        // Spawn user in ECS
        var entity = Prototyper.Clone(World, characterModel.Type);
        World.Set(entity, new Identity((int)characterModel.Id!, characterModel.Type));
        World.Set(entity, new TerraBound.Core.Components.Character(characterModel.Username, request.Peer));
        World.Set(entity, new NetworkedTransform(characterModel.TransformModel.Position));
        entityMapper[(int)characterModel.Id] = entity;
        
        // Spawn user on client 
        var teleportCommand = new TeleportCommand{ Position = characterModel.TransformModel.Position };
        var spawnCommand = new SpawnCommand { Id = 1, Type = "", Position = characterModel.TransformModel.Position };
        serverNetworkService.Send(request.Peer, ref teleportCommand);
        serverNetworkService.Send(request.Peer, ref spawnCommand);
    }
}