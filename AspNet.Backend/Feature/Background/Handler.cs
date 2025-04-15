using Arch.Buffer;
using Arch.Core;
using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Background.Systems;
using LiteNetLib;
using Microsoft.IdentityModel.Tokens;
using TerraBound.Core.Components;
using TerraBound.Core.Network;

namespace AspNet.Backend.Feature.Background;

/// <summary>
/// The <see cref="NetworkHandler"/>
/// reacts to networking events, processes them and redirects them to the ecs if necessary.
/// </summary>
/// <param name="logger"></param>
/// <param name="commandBuffer"></param>
public class NetworkHandler(
    ILogger<NetworkHandler> logger, 
    CommandBuffer commandBuffer
)
{
    /// <summary>
    /// Gets called when a player was successfully connected. 
    /// </summary>
    /// <param name="peer">The <see cref="NetPeer"/>.</param>
    public void OnConnected(NetPeer peer)
    {
        // Extract userId
        var userId = peer.Tag as string;
        if (userId.IsNullOrEmpty())
        {
            logger.LogError($"User id of peer {peer} is null or empty.");
            return;
        }

        // Create event entity 
        var bufferedEntity = commandBuffer.Create([typeof(OnConnectionEstablished)]);
        commandBuffer.Set(bufferedEntity, new OnConnectionEstablished{ UUID = userId!, Peer = peer });
    }

    /// <summary>
    /// Gets called once a player double clicks. 
    /// </summary>
    /// <param name="clickCommand">The <see cref="DoubleClickCommand"/>.</param>
    /// <param name="peer">The <see cref="NetPeer"/>.</param>
    public void OnDoubleClick(DoubleClickCommand clickCommand, NetPeer peer)
    {
        // Create event entity 
        var bufferedEntity = commandBuffer.Create([typeof(DoubleClickCommand)]);
        commandBuffer.Set(bufferedEntity, clickCommand);
    }
}