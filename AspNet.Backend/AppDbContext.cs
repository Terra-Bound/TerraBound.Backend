using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Character;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AspNet.Backend;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>()
            .HasOne(u => u.CharacterModel)
            .WithOne()
            .HasForeignKey<CharacterModel>(p => p.User) 
            .OnDelete(DeleteBehavior.Cascade); 
        
        // Player config
        modelBuilder.Entity<CharacterModel>().HasKey(x => x.Id);
        modelBuilder.Entity<CharacterModel>()
            .Property(p => p.Username)
            .HasMaxLength(16)
            .IsRequired();
        modelBuilder.Entity<CharacterModel>()
            .Property(p => p.Type)
            .HasMaxLength(16)
            .IsRequired();
        modelBuilder.Entity<CharacterModel>()
            .OwnsOne(p => p.TransformModel); 
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
            };

            var result = await userManager.CreateAsync(adminUser, "AdminPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Feature.Authentication.Roles.ADMIN.ToString());
            }
        }
    }
    
    public DbSet<CharacterModel> Players { get; set; }
}
