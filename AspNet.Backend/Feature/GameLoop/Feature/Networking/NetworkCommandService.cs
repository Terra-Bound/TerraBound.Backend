using System.Numerics;
using LiteNetLib;
using TerraBound.Core.Network;

namespace AspNet.Backend.Feature.GameLoop.Feature.Networking;

public class NetworkCommandService
(
    ILogger<NetworkEventsService> logger,
    ServerNetworkService serverNetworkService 
)
{

    public void SendSpawnAndCenterOnMapCommand(NetPeer peer, int id, string type, Vector2 position)
    {
        var teleportCommand = new TeleportCommand{ Position = position };
        var spawnCommand = new SpawnCommand { Id = id, Type = type, Position = position };
        serverNetworkService.Send(peer, ref teleportCommand);
        serverNetworkService.Send(peer, ref spawnCommand);
    }
}