using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Shared;

namespace AspNet.Backend.Feature.Character;

public class CharacterService(ILogger<CharacterService> logger, AppDbContext context)
{
    /// <summary>
    /// Creates an <see cref="CharacterModel"/> for an <see cref="User"/>. 
    /// </summary>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="username">The username for the player.</param>
    /// <returns></returns>
    public async Task<CharacterModel> CreateCharacterAsync(User user, string username)
    {
        var player = new CharacterModel
        {
            Id = null,
            Username = username,
            Type = "char:1",
            TransformModel = TransformModel.Zero,
            User = user,
        };

        context.Players.Add(player);
        user.CharacterModel = player;
        await context.SaveChangesAsync();
        
        logger.LogInformation($"Player created with ID {player!.Id} by {user.Id}");
        return player;
    }
}