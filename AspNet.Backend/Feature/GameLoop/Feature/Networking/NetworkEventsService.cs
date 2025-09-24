using Arch.Buffer;
using AspNet.Backend.Feature.GameLoop.Feature.Shared;
using LiteNetLib;
using TerraBound.Core.Components;
using TerraBound.Core.Network;

namespace AspNet.Backend.Feature.GameLoop.Feature.Networking;

/// <summary>
/// The <see cref="NetworkEventsService"/>
/// reacts to networking events, processes them and redirects them to the ecs if necessary.
/// </summary>
/// <param name="logger"></param>
/// <param name="commandBuffer"></param>
public class NetworkEventsService(
    ILogger<NetworkEventsService> logger, 
    CommandBuffer commandBuffer,
    EntityMapper mapper,
    UserIdToEntityMapper userIdToEntityMapper
)
{
    /// <summary>
    /// Gets called when a player was successfully connected. 
    /// </summary>
    /// <param name="peer">The <see cref="NetPeer"/>.</param>
    public void OnConnected(NetPeer peer)
    {
        // Error, userid is null, prevent further escalation.
        if (peer.Tag is string uuid && string.IsNullOrEmpty(uuid))
        {
            logger.LogError($"User id of peer {peer} is null or empty.");
            return;
        }
        
        // Check if entity exists, if yes, reconnect
        if (userIdToEntityMapper.TryGetValue((peer.Tag as string)!, out var entity))
        {
            var bufferedEntity = commandBuffer.Create([typeof(OnReconnected), typeof(Command), typeof(Destroy)]);
            commandBuffer.Set(bufferedEntity, new OnReconnected{ EntityId = entity.Id, Peer = peer });
        }
        // Else create user
        else
        {
            var bufferedEntity = commandBuffer.Create([typeof(OnConnectionEstablished), typeof(Command), typeof(Destroy)]);
            commandBuffer.Set(bufferedEntity, new OnConnectionEstablished{ UUID = (string)peer.Tag!, Peer = peer });
        }
    }

    /// <summary>
    /// Gets called when a player was successfully connected. 
    /// </summary>
    /// <param name="peer">The <see cref="NetPeer"/>.</param>
    /// <param name="info">The <see cref="DisconnectInfo"/>.</param>
    public void OnDisconnected(NetPeer peer, DisconnectInfo info)
    {
        // Extract userId
        var uuid = (string)peer.Tag;
        
        // Get entity and mark it for destruction
        if (!userIdToEntityMapper.TryGetValue(uuid, out var entity))
        {
            logger.LogError(Error.EntityNotFound);
            return;
        }
        
        var bufferedEntity = commandBuffer.Create([typeof(OnConnectionLost), typeof(Command), typeof(Destroy)]);
        commandBuffer.Set(bufferedEntity, new OnConnectionLost{ EntityId = entity.Id, Peer = peer });
    }

    /// <summary>
    /// Gets called once a player double clicks. 
    /// </summary>
    /// <param name="clickCommand">The <see cref="DoubleClickCommand"/>.</param>
    /// <param name="peer">The <see cref="NetPeer"/>.</param>
    public void OnDoubleClick(DoubleClickCommand clickCommand, NetPeer peer)
    {
        // Create event entity 
        var bufferedEntity = commandBuffer.Create([typeof(DoubleClickCommand), typeof(Command), typeof(Destroy)]);
        commandBuffer.Set(bufferedEntity, clickCommand);
        
    }
}