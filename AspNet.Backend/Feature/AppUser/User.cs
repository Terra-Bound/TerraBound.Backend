using Microsoft.AspNetCore.Identity;
using Player = AspNet.Backend.Feature.Player.Player;

namespace AspNet.Backend.Feature.AppUser;

/// <summary>
/// The <see cref="CreateOrUpdateUserDto"/> struct
/// contains all required data to create or update an <see cref="User"/>.
/// </summary>
public struct CreateOrUpdateUserDto
{
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

/// <summary>
/// The <see cref="UserDto"/> struct
/// represents an <see cref="User"/> with all his transferable data.
/// </summary>
public struct UserDto
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public DateTimeOffset RegisterDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}

/// <summary>
/// The <see cref="User"/> struct
/// is based on <see cref="IdentityUser"/>, acts as an user and contains all his data.
/// </summary>
public class User : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime RegisterDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    
    public Player.Player Player { get; set; }
}