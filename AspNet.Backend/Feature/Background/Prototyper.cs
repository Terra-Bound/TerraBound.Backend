using System.Numerics;
using Arch.Core;
using TerraBound.Core.Components;
namespace AspNet.Backend.Feature.Background;

public static class Prototyper
{
    public static Entity Clone(World world, string type)
    {
        switch (type)
        {
            case "char:1":
                world.Create(
                    new Identity(-1, type),
                    new TerraBound.Core.Components.Character(), 
                    new NetworkedTransform(Vector2.Zero), 
                    new Velocity(Vector2.Zero),
                    new Movement(Vector2.Zero, 2f),
                    new Toggle<DirtyTransform>(false)
                );
                break;
            default:
                world.Create();
                break;
        }

        return world.Create();
    }
}