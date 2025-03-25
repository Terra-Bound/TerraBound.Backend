using System.ComponentModel.DataAnnotations;

namespace AspNet.Backend.Feature.Shared;

/// <summary>
/// The <see cref="Identity"/> class
/// represents an embedded class or entity that stores the identity of an entity with its type and tag.      
/// </summary>
public class Identity
{
    [Key] public long Id { get; set; }
    public required string Type { get; set; }
}