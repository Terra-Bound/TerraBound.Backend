/*
using System.Net.Http.Json;
using AspNet.Backend.Feature.AppUser;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspNet.Backend.Tests;

public class UsersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly AppDbContext _context;

    public UsersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // WebApplicationFactory konfigurieren
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove postgres for test
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // In-Memory-Database for tests
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });
                
                // Remove auth for test
                services.AddAuthentication("TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .AddAuthenticationSchemes("TestScheme")
                        .RequireAuthenticatedUser()
                        .Build();
                });
            });
        });

        _client = _factory.CreateClient(); // HttpClient für API-Tests

        // Wir erstellen einen neuen Scope, um auf den DbContext zuzugreifen
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        // Arrange: Füge Testdaten hinzu
        _context.Users.Add(new User { Id = "1", FirstName = "John", LastName = "Doe", Email = "john@example.com", PasswordHash = "123" });
        _context.Users.Add(new User { Id = "2", FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", PasswordHash = "456" });
        await _context.SaveChangesAsync();

        // Act: HTTP GET request an den Endpoint
        var response = await _client.GetAsync("/api/Users");

        // Assert
        response.EnsureSuccessStatusCode();  // Status-Code ist 2xx
        var users = await response.Content.ReadFromJsonAsync<List<User>>();
        Assert.Equal(2, users.Count);  // Es gibt zwei Benutzer
    }

    [Fact]
    public async Task PostUser_UpdateUserInDatabase()
    {
        // Arrange: Existierender Benutzer in der Datenbank
        var existingUser = new User
        {
            FirstName = "Alice",
            LastName = "Wonderland",
            Email = "alice@example.com",
            PasswordHash = "hashed789"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        // Neue Daten für das Update
        var updatedUser = new CreateOrUpdateUserDto
        {
            FirstName = "Alicia",
            LastName = "Wonder",
            Email = "alice@example.com",
            Password = "newpassword123"
        };

        // Act: HTTP PUT request an den Endpoint
        var response = await _client.PutAsJsonAsync($"/api/Users/{existingUser.Id}", updatedUser);

        // Assert
        response.EnsureSuccessStatusCode(); // Status-Code ist 2xx

        // Überprüfe, ob der Benutzer aktualisiert wurde
        var userInDb = await _context.Users.FindAsync(existingUser.Id);
        Assert.NotNull(userInDb);
        Assert.Equal("Alicia", userInDb.FirstName);
        Assert.Equal("Wonder", userInDb.LastName);
    }

    [Fact]
    public async Task DeleteUser_RemovesUserFromDatabase()
    {
        // Arrange: Füge einen Benutzer hinzu, den wir löschen können
        var user = new User { FirstName = "John", LastName = "Doe", Email = "john@example.com", PasswordHash = "123" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act: HTTP DELETE request an den Endpoint
        var response = await _client.DeleteAsync($"/api/Users/{user.Id}");

        // Assert
        response.EnsureSuccessStatusCode(); // Status-Code ist 2xx
        var userInDb = await _context.Users.FindAsync(user.Id);
        Assert.Null(userInDb); // Benutzer sollte nicht mehr in der DB sein
    }
}
*/

