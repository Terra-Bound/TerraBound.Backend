using System.Security.Claims;
using System.Threading.Channels;
using AspNet.Backend.Feature.Authentication;
using LiteNetLib;
using LiteNetLib.Utils;
using TerraBound.Core;
using TerraBound.Core.Network;

namespace AspNet.Backend.Feature.Background;

public delegate void OnConnectionRequest(ConnectionRequest connectionRequest);

public delegate void OnConnected(NetPeer peer);

public delegate void OnReceive(NetPeer peer, NetDataReader reader, byte channel, DeliveryMethod method);

public delegate void OnDisconnected(NetPeer peer, DisconnectInfo info);

/// <summary>
/// The <see cref="ServerNetworkService"/> class
/// represents the networking code for the server.
/// </summary>
public class ServerNetworkService(
    ILogger<ServerNetworkService> logger, AuthenticationService authenticationService
) : Network {
    
    private const ushort MaxConnections = 10;

    /// <summary>
    ///     Gets invoked once a connection request came in
    /// </summary>
    public OnConnectionRequest OnConnectionRequest { get; set; }
    
    /// <summary>
    ///     Gets invoked once a connection request was approved and the connection was established.
    /// </summary>
    public OnConnected OnConnected { get; set; }

    /// <summary>
    ///     Gets invoked once a packet was received.
    /// </summary>
    public OnReceive OnReceive { get; set; }
    
    /// <summary>
    ///     Gets invoked once a user connection is being disconnected
    /// </summary>
    public OnDisconnected OnDisconnected { get; set; }
    
    protected override void Setup()
    {
        base.Setup();

        // Setting the delegates, otherhwise invoking them causes null pointer exceptions
        OnConnectionRequest = request => { };
        OnReceive = (peer, reader, channel, method) => { };
        OnConnected = (peer) => { };
        OnDisconnected = (peer, info) => { };

        OnConnectionRequest += ApproveConnection;

        Listener.ConnectionRequestEvent += request => OnConnectionRequest(request);
        Listener.PeerConnectedEvent += peer => OnConnected(peer);
        Listener.NetworkReceiveEvent += (peer, reader, channel, method) => OnReceive(peer, reader, channel, method);
        Listener.PeerDisconnectedEvent += (peer, info) => OnDisconnected(peer, info);
    }

    /// <summary>
    ///     Approves an incoming connection if the <see cref="MaxConnections" /> wasnt reached and the JWT-Token is valid.
    /// </summary>
    /// <param name="request">The request.</param>
    private void ApproveConnection(ConnectionRequest request)
    {
        if (Manager.ConnectedPeersCount < MaxConnections)
        {
            // Verify jwt token
            var jwt = request.Data.ToString();
            if (authenticationService.IsValidJwtToken(jwt, out var claims))
            {
                var userId = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var peer = request.Accept();
                peer.Tag = userId;
                
                logger.LogDebug($"User: {userId} established connection.");
            } 
            else request.Reject();
        }
        else request.Reject();
    }
}