using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Shared;
using EFCore.BulkExtensions;

namespace AspNet.Backend.Feature.Character;

/// <summary>
/// The <see cref="CharacterService"/> class
/// is responsible for creating, destroying and managing character entities.
/// </summary>
/// <param name="logger">The <see cref="Logger{T}"/>.</param>
/// <param name="context">The <see cref="AppDbContext"/>.</param>
public class CharacterService(ILogger<CharacterService> logger, AppDbContext context)
{
    private readonly List<CharacterModel> _characterModels = new();
    
    /// <summary>
    /// Creates an <see cref="CharacterModel"/> for an <see cref="User"/>. 
    /// </summary>
    /// <param name="userUuid">The <see cref="User"/> uuid.</param>
    /// <param name="username">The username for the player.</param>
    /// <returns></returns>
    public async Task<CharacterDto> CreateCharacterAsync(string userUuid, string username)
    {
        // Stub to prevent another database operation or lookup in ef core. 
        var stubUser = new User { Id = userUuid };
        context.Users.Attach(stubUser);
        
        var player = new CharacterModel
        {
            Identity = new IdentityModel{ Type = "char:1"},
            Username = username,
            Transform = TransformModel.Zero,
            UserId = userUuid,
            User = stubUser
        };

        context.Characters.Add(player);
        stubUser.Character = player;
        await context.SaveChangesAsync();
        
        logger.LogInformation("Player created with ID {PlayerId} by {UserId}", player!.Identity.Id, userUuid);
        return player.ToDto();
    }
    
    /// <summary>
    /// Updates a <see cref="CharacterModel"/> in bulk, adds it to the local list and updates all of them at once during <see cref="SaveChangesInBulkAsync"/>.
    /// </summary>
    /// <param name="characterDto">The instance.</param>
    public void UpdateInBulkAsync(CharacterDto characterDto)
    {
        var model = characterDto.ToEntity();
        _characterModels.Add(model);
    }

    /// <summary>
    /// Updates all tracked instances. 
    /// </summary>
    public async ValueTask SaveChangesInBulkAsync()
    {
        await context.BulkInsertOrUpdateAsync(_characterModels);
        _characterModels.Clear();
    }
}