using Arch.Core;

namespace AspNet.Backend.Feature.GameLoop.Feature.Entity;

public static class Prototyper
{
    public static Arch.Core.Entity Clone(World world, string type)
    {
        switch (type)
        {
            case "char:1":
                return CharacterEntityService.CreateTemplate(world);
            case "chunk:1":
                return ChunkEntityService.CreateTemplate(world);
            default:
                return world.Create();
        }
    }
}