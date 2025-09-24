using System.Numerics;
using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Character;
using AspNet.Backend.Feature.Chunk;
using AspNet.Backend.Feature.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TerraBound.Core.Geo;

namespace AspNet.Backend;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<ChunkModel> Chunks { get; set; }
    public DbSet<CharacterModel> Characters { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseIdentityByDefaultColumns();
        
        // User config
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(u => u.Character).WithOne(c => c.User).HasForeignKey<CharacterModel>(p => p.UserId).OnDelete(DeleteBehavior.Cascade); 
            entity.Navigation(e => e.Character).AutoInclude();
        });
        
        
        // Identity config
        modelBuilder.Entity<IdentityModel>(entity => { });
        
        // Character config
        modelBuilder.Entity<CharacterModel>(entity =>
        {
            // Key, references identity
            entity.HasKey(character => character.IdentityId);
            entity.HasOne(e => e.Identity).WithOne().HasForeignKey<CharacterModel>(e => e.IdentityId);
            entity.Navigation(character => character.Identity).AutoInclude();
            
            // Properties
            entity.Property(p => p.Username).HasMaxLength(16).IsRequired();
            entity.OwnsOne(p => p.Transform); 
            
            // Relations
            entity.Navigation(c => c.User).AutoInclude();
        });
        
        // Chunk config
        modelBuilder.Entity<ChunkModel>(entity =>
        {
            // Key, references identity
            entity.HasKey(chunkModel => chunkModel.IdentityId);
            entity.HasOne(e => e.Identity).WithOne().HasForeignKey<ChunkModel>(e => e.IdentityId);
            entity.Navigation(chunkModel => chunkModel.Identity).AutoInclude();
            
            // Index for performance
            entity.HasIndex(c => new { c.X, c.Y }).IsUnique();
            
            // Relations
            entity.Navigation(c => c.Characters).AutoInclude();
        });
    }
    
    /// <summary>
    ///     Seed users and roles in the Identity database.
    /// </summary>
    /// <param name="userManager">ASP.NET Core Identity User Manager</param>
    /// <param name="roleManager">ASP.NET Core Identity Role Manager</param>
    /// <returns></returns>
    public static async Task SeedAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync(Feature.Authentication.Roles.ADMIN.ToString()))
        {
            await roleManager.CreateAsync(new IdentityRole(Feature.Authentication.Roles.ADMIN.ToString()));
        }

        if (!await roleManager.RoleExistsAsync(Feature.Authentication.Roles.USER.ToString()))
        {
            await roleManager.CreateAsync(new IdentityRole(Feature.Authentication.Roles.USER.ToString()));
        }
        
        if (await userManager.FindByNameAsync("Admin") == null)
        {
            
            // Create user
            var adminUser = new User
            {
                UserName = "Admin",
                NormalizedUserName = "Admin".ToUpper(),
                FirstName = "Admin",
                LastName = "",
                Email = "admin@example.com",
                NormalizedEmail = "admin@example.com".ToUpper(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                RegisterDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Character = null
            };

            var result = await userManager.CreateAsync(adminUser, "AdminPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Feature.Authentication.Roles.ADMIN.ToString());

                // Identity
                var identity = new IdentityModel
                {
                    Type = "char:1"
                };
                
                // Create character
                var character = new CharacterModel
                {
                    Identity = identity,
                    
                    Username = "Admin",
                    Transform = new TransformModel
                    {
                        Position = GeoUtils.Wgs84ToWebMercator(8.318016f,51.839082f),  // X of the result is the lon, y the lat 
                        Rotation = Vector2.Zero,
                    },
                    
                    UserId = adminUser.Id,
                    User = adminUser
                };
                adminUser.Character = character;
                await userManager.UpdateAsync(adminUser);
            }
        }
    }
}
