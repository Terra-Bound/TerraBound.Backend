using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace AspNet.Backend.Feature.Shared;

/// <summary>
/// The <see cref="TransformModel"/> class
/// includes <see cref="Position"/> and <see cref="Rotation"/>. 
/// </summary>
[Owned] 
public class TransformModel
{
    public static TransformModel Zero = new() { Position = Vector2.Zero, Rotation = Vector2.Zero };
    
    public Vector2 Position { get; set; }
    public Vector2 Rotation { get; set; }
}