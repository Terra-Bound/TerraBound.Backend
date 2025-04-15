using Arch.Core;
using Arch.System;
using AspNet.Backend.Feature.AppUser;
using TerraBound.Core.Components;
using TerraBound.Core.Network;

namespace AspNet.Backend.Feature.Background.Systems;

/// <summary>
/// The <see cref="CommandGroup"/>
/// is a system listening to certain events or networking commands to apply them to the ecs. 
/// </summary>
/// <param name="world">The <see cref="World"/>.</param>
/// <param name="entityMapper">The <see cref="EntityMapper"/>.</param>
public sealed partial class CommandGroup(
    World world, 
    EntityMapper entityMapper
) : BaseSystem<World, float>(world) 
{
    [Query]
    private void OnDoubleClickMoveCharacter(in DoubleClickCommand command)
    {
        var entity = entityMapper[command.Id];
        ref var movement = ref world.Get<Movement>(entity);
        movement.Target = command.Position;
    }
}