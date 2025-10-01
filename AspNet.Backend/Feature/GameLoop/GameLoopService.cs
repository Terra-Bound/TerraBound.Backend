using System.Diagnostics;
using Arch.Core;
using Arch.System;
using AspNet.Backend.Feature.GameLoop.Feature.Entity;
using AspNet.Backend.Feature.GameLoop.Feature.Networking;
using AspNet.Backend.Feature.GameLoop.Feature.Shared;
using AspNet.Backend.Feature.GameLoop.Group;
using Core.Systems;
using TerraBound.Core.Network;
using MovementSystem = AspNet.Backend.Feature.GameLoop.Group.MovementSystem;
using NetworkSystem = AspNet.Backend.Feature.GameLoop.Group.NetworkSystem;

namespace AspNet.Backend.Feature.GameLoop;

/// <summary>
/// The <see cref="GameLoopService"/> class
/// is a <see cref="BackgroundService"/> running at a given <see cref="TickRateMs"/> and acts as the game-server. 
/// </summary>
public class GameLoopService : BackgroundService
{
    private readonly ILogger<GameLoopService> _logger;
    private readonly ILogger<DatabaseGroup> _dbLogger;
    private readonly ILogger<CharacterEntityService> _characterLogger;
    
    /// <summary>
    /// The world.
    /// </summary>
    private readonly World _world;
    private readonly EntityMapper _entityMapper;
    private readonly UserIdToEntityMapper _userIdToEntityMapper;
    private readonly ChunkMapper _chunkMapper;
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
    private readonly NetworkEventsService _networkEventsService;
    private readonly NetworkCommandService _networkCommandService;
    
    // Entity services
    private readonly EntityService _entityService;
    private readonly CharacterEntityService _characterEntityService;
    private readonly ChunkEntityService _chunkEntityService;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
    /// <param name="networkLogger">The <see cref="ILogger{TCategoryName}"/> for the <see cref="NetworkEventsService"/>.</param>
    /// <param name="dbLogger">The <see cref="ILogger{TCategoryName}"/> for the <see cref="DatabaseGroup"/>.</param>
    /// <param name="characterLogger">The <see cref="ILogger{TCategoryName}"/> for the <see cref="CharacterEntityService"/>.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to create scoped services.</param>
    /// <param name="networkService">The <see cref="ServerNetworkService"/>, a singleton used to manage socket connections to the players.</param>
    public GameLoopService(
        ILogger<GameLoopService> logger, 
        ILogger<NetworkEventsService> networkLogger,
        ILogger<DatabaseGroup> dbLogger,
        ILogger<CharacterEntityService> characterLogger,
        ILogger<ChunkEntityService> chunkEntityLogger,
        IServiceProvider serviceProvider, 
        ServerNetworkService networkService
    ) {
        _logger = logger;
        
        // World
        _world = World.Create();
        _entityMapper = new EntityMapper();
        _chunkMapper = new ChunkMapper();
        _userIdToEntityMapper = new UserIdToEntityMapper();
        _eventCommandBufferSystem = new EntityCommandBufferSystem(_world);
        _entityCommandBufferSystem = new EntityCommandBufferSystem(_world);
        
        // Network
        _networkService = networkService;
        _networkEventsService = new NetworkEventsService(networkLogger, _eventCommandBufferSystem.EntityCommandBuffer, _entityMapper, _userIdToEntityMapper);
        _networkCommandService = new NetworkCommandService(networkLogger, _networkService);
        _networkService.Port = 9050;
        _networkService.Start();
        _networkService.OnConnected += _networkEventsService.OnConnected;
        _networkService.OnDisconnected += _networkEventsService.OnDisconnected;
        _networkService.OnReceive<DoubleClickCommand>(_networkEventsService.OnDoubleClick, () => new DoubleClickCommand());
        
        // Services
        _entityService = new EntityService(_logger, _world, _entityMapper);
        _characterEntityService = new CharacterEntityService(characterLogger, _world, _entityService, _userIdToEntityMapper);
        _chunkEntityService = new ChunkEntityService(chunkEntityLogger, _world, _entityService, _chunkMapper);
        
        // System setup
        _systems = new Group<float>(
            "Systems",
            _eventCommandBufferSystem,
            new StageGroup(_logger, serviceProvider, _world, _entityMapper, _entityService, _characterEntityService, _chunkEntityService, _networkCommandService),
            new KeepAliveGroup(_logger, _world),
            _entityCommandBufferSystem,
            new ReactiveSystem(_world),
            new MovementSystem(_logger, _world),
            new NetworkSystem(_world, networkService),
            new DatabaseGroup(dbLogger, serviceProvider, _world),
            new UnstageGroup(_logger, _world, _characterEntityService, _chunkEntityService)
        );
        _systems.Initialize();
    }
    
    /// <summary>
    /// The gameloop itself, running in the background. 
    /// </summary>
    /// <param name="stoppingToken">The <see cref="CancellationToken"/> to stop the gameloop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Server starting...");

        // Stopwatch for tracking time & delta time
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var previousTime = stopwatch.Elapsed;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            // Deltatime
            var currentTime = stopwatch.Elapsed;
            var deltaTime = (float)(currentTime - previousTime).TotalSeconds;
            previousTime = currentTime;
        
            // Update game 
            try
            {
                UpdateGameLogic(deltaTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        
            // Calculate remaining time to next tick
            var elapsedMs = stopwatch.ElapsedMilliseconds - currentTime.TotalMilliseconds;
            var delay = Math.Max(0, TickRateMs - elapsedMs);
            await Task.Delay((int)delay, stoppingToken);
        }

        _logger.LogInformation("Server stopped...");
    }

    /// <summary>
    /// Runs in the gameloop to update the server gamestate.
    /// </summary>
    /// <param name="deltaTime">Die seit dem letzten Update vergangene Zeit in Sekunden.</param>
    private void UpdateGameLogic(float deltaTime)
    {
        //_logger.LogInformation("DeltaTime: {DeltaTime}", deltaTime);
        _networkService.Update();       
        _systems.BeforeUpdate(deltaTime);
        _systems.Update(deltaTime);
        _systems.AfterUpdate(deltaTime);
        _networkService.Manager.TriggerUpdate();
    }
}
