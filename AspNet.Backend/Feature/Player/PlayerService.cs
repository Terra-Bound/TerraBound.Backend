using AspNet.Backend.Feature.AppUser;

namespace AspNet.Backend.Feature.Player;

public class PlayerService(ILogger<PlayerService> logger, AppDbContext context)
{
    
    /// <summary>
    /// Creates an <see cref="Player"/> for an <see cref="User"/>. 
    /// </summary>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="username">The username for the player.</param>
    /// <returns></returns>
    public async Task<Player> CreatePlayerAsync(User user, string username)
    {
        var player = new Player
        {
            Id = null,
            Username = username,
            Type = "char:1",
        };

        context.Players.Add(player);
        user.Player = player;
        await context.SaveChangesAsync();
        
        logger.LogInformation($"Player created with ID {player!.Id} by {user.Id}");
        return player;
    }
}