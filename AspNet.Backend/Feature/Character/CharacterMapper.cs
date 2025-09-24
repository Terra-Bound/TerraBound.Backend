using System.Diagnostics.CodeAnalysis;
using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Shared;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.Character;

#pragma warning disable RMG020

/// <summary>
/// The <see cref="UserMapper"/> class
/// defines a mapper that automatically generates code to convert and update between the <see cref="User"/> and his Dtos.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class CharacterMapper
{
    /// <summary>
    /// Converts an <see cref="CharacterModel"/> to its <see cref="CharacterDto"/>.
    /// </summary>
    /// <param name="user">The <see cref="CharacterModel"/>.</param>
    /// <returns>The converted <see cref="CharacterDto"/>.</returns>
    [MapProperty(nameof(CharacterModel.IdentityId), nameof(CharacterDto.Id))]
    [MapProperty(nameof(CharacterModel.Identity.Type), nameof(CharacterDto.Type))]
    [MapProperty(nameof(CharacterModel.Username), nameof(CharacterDto.Username))]
    public static partial CharacterDto ToDto(this CharacterModel user);

    /// <summary>
    /// Converts multiple components to its <see cref="CharacterDto"/>.
    /// </summary>
    /// <param name="identity">Its <see cref="Identity"/>.</param>
    /// <param name="character">Its <see cref="Character"/>.</param>
    /// <param name="transform">Its <see cref="NetworkedTransform"/>.</param>
    /// <returns>The converted <see cref="CharacterDto"/>.</returns>
    public static CharacterDto ToDto(Identity identity, TerraBound.Core.Components.Character character, NetworkedTransform transform)
    {
        // Create dto
        return new CharacterDto()
        {
            Id = identity.Id,
            Type = identity.Type,
            UserId = character.GUID,
            Username = character.Name,
            Transform = transform,
        };
    }
    
    /// <summary>
    /// Converts an <see cref="CharacterDto"/> to its <see cref="CharacterModel"/>.
    /// </summary>
    /// <param name="dto">The <see cref="CharacterDto"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <returns>The converted <see cref="CharacterModel"/>.</returns>
    [MapProperty(nameof(CharacterDto.Id), nameof(CharacterModel.IdentityId))]
    [MapProperty(nameof(CharacterDto.Type), nameof(CharacterModel.Identity.Type))]
    [MapProperty(nameof(CharacterDto.UserId), nameof(CharacterModel.User.Id))]
    [MapValue(nameof(CharacterModel.Identity), Use = nameof(GetStubIdentity))]  // Alternative pass Identity and User via parameter to the ToEntity method
    [MapValue(nameof(CharacterModel.User), Use = nameof(GetStubUser))]
    public static partial CharacterModel ToEntity(this CharacterDto dto);

    /// <summary>
    /// Returns an empty <see cref="IdentityModel"/> since it is required in the constructor. 
    /// </summary>
    /// <returns>An empty <see cref="IdentityModel"/>.</returns>
    private static IdentityModel GetStubIdentity()
    {
        return new IdentityModel();
    }
    
    /// <summary>
    /// Returns an empty <see cref="User"/> since it is required in the constructor. 
    /// </summary>
    /// <returns>An empty <see cref="User"/>.</returns>
    private static User GetStubUser()
    {
        return new User();
    }
}

#pragma warning restore RMG020