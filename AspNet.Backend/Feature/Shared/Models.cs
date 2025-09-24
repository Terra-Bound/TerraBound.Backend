using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using TerraBound.Core.Components;

namespace AspNet.Backend.Feature.Shared;

/// <summary>
/// The <see cref="Identity"/> class
/// represents a unique entity identity in our database. 
/// </summary>
public class IdentityModel
{
    [Key] public int Id { get; set; }
    public string Type { get; set; }
}

/// <summary>
/// The <see cref="TransformModel"/> class
/// includes <see cref="Position"/> and <see cref="Rotation"/>. 
/// </summary>
[Owned] 
public class TransformModel
{
    public static TransformModel Zero = new() { Position = Vector2.Zero, Rotation = Vector2.Zero };
    
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    
    [NotMapped]
    public Vector2 Position
    {
        get => new Vector2(PositionX, PositionY);
        set
        {
            PositionX = value.X;
            PositionY = value.Y;
        }
    }

    [NotMapped]
    public Vector2 Rotation
    {
        get => new Vector2(RotationX, RotationY);
        set
        {
            RotationX = value.X;
            RotationY = value.Y;
        }
    }
}