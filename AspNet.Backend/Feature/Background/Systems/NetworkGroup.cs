using System.Numerics;
using Arch.Core;
using Arch.System;
using LiteNetLib;
using TerraBound.Core;
using TerraBound.Core.Components;
using TerraBound.Core.Network;

namespace AspNet.Backend.Feature.Background.Systems;

/// <summary>
/// The <see cref="NetworkSystem"/>
/// is a system synchronizing the state with the client by sending packages. 
/// </summary>
/// <param name="world"></param>
/// <param name="serverNetworkService"></param>
public sealed partial class NetworkSystem(World world, ServerNetworkService serverNetworkService) : BaseSystem<World, float>(world)
{
    [Query]
    private void SendCharacterData(in Identity identity, in TerraBound.Core.Components.Character character, in NetworkedTransform transform, in Toggle<DirtyTransform> toggle)
    {
        if (!toggle.Enabled) return;

        var entityCommand = new PositionCommand { Id = identity.Id, Position = transform.Position };
        serverNetworkService.Send(character.Peer, ref entityCommand, DeliveryMethod.Sequenced);
    }
    
    [Query]
    private void SendTransform(in Identity identity, in TerraBound.Core.Components.Character character, in NetworkedTransform transform, in Toggle<DirtyTransform> toggle)
    {
        if (!toggle.Enabled) return;

        var entityCommand = new PositionCommand { Id = identity.Id, Position = transform.Position };
        serverNetworkService.Send(character.Peer, ref entityCommand, DeliveryMethod.Sequenced);
    }
}