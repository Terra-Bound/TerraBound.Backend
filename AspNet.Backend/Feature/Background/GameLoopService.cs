using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Arch.System;
using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Background.Systems;
using LiteNetLib;
using Microsoft.IdentityModel.Tokens;
using TerraBound.Core;
using TerraBound.Core.Components;
using TerraBound.Core.Network;

namespace AspNet.Backend.Feature.Background;

/// <summary>
/// The <see cref="GameLoopService"/> class
/// is a <see cref="BackgroundService"/> running at a given <see cref="TickRateMs"/> and acts as the game-server. 
/// </summary>
public class GameLoopService : BackgroundService
{
    private readonly ILogger<GameLoopService> _logger;
    
    /// <summary>
    /// The world.
    /// </summary>
    private readonly World _world;
    private readonly EntityMapper _entityMapper;
    private readonly EntityCommandBufferSystem _eventCommandBufferSystem;
    private readonly EntityCommandBufferSystem _entityCommandBufferSystem;
    private readonly Group<float> _systems;
    
    /// <summary>
    /// The tickrate at which the game-server is running at.
    /// </summary>
    private const int TickRateMs = 1000 / 60; // 60Hz = 16.67ms per tick
    
    /// <summary>
    /// The network, used to receive and send packets to the players. 
    /// </summary>
    private readonly ServerNetworkService _networkService;
    private readonly NetworkHandler _networkHandler;
    
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
    /// <param name="userService">The <see cref="UserService"/>.</param>
    /// <param name="networkService">The <see cref="ServerNetworkService"/>.</param>
    public GameLoopService(ILogger<GameLoopService> logger, UserService userService, ServerNetworkService networkService) 
    {
        _logger = logger;
        
        // World
        _world = World.Create();
        _entityMapper = new EntityMapper();
        _eventCommandBufferSystem = new EntityCommandBufferSystem(_world);
        _entityCommandBufferSystem = new EntityCommandBufferSystem(_world);
        
        // Network
        _networkService = networkService;
        _networkHandler = new NetworkHandler(null, _eventCommandBufferSystem.EntityCommandBuffer);
        _networkService.Start();
        _networkService.OnConnected += _networkHandler.OnConnected;
        _networkService.OnReceive<DoubleClickCommand>(_networkHandler.OnDoubleClick, () => new DoubleClickCommand());
        
        // System setup
        _systems = new Group<float>(
            "Systems",
            _eventCommandBufferSystem,
            new CommandGroup(_world, _entityMapper),
            new InitialisationGroup(_world, userService, networkService, _entityMapper),
            _entityCommandBufferSystem,
            new MovementSystem(_world),
            new NetworkSystem(_world, networkService)
        );
    }
    
    /// <summary>
    /// The gameloop itself, running in the background. 
    /// </summary>
    /// <param name="stoppingToken">The <see cref="CancellationToken"/> to stop the gameloop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Server starting...");
        
        var stopwatch = new Stopwatch();
        while (!stoppingToken.IsCancellationRequested)
        {
            stopwatch.Restart();
            
            try
            {
                UpdateGameLogic();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var delay = Math.Max(0, TickRateMs - elapsedMs);

            await Task.Delay((int)delay, stoppingToken);
        }

        _logger.LogInformation("Server stopped...");
    }

    /// <summary>
    /// Runs in the gameloop to update the server gamestate.
    /// </summary>
    private void UpdateGameLogic()
    {
        //logger.LogInformation("Tick: {Time}", DateTimeOffset.Now);
        _networkService.Update();                   // Receives/Polls incoming packets
        _networkService.Manager.TriggerUpdate();    // Sends packets async
    }
}
