// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.Disease.Components;
using Content.Shared._Modifications.Disease;
using Robust.Client.GameObjects;

namespace Content.Client._Modifications.Disease.Systems;

public sealed class DiseaseMutationSystem : VisualizerSystem<DiseaseMutationComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, DiseaseMutationComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, DiseaseMutationVisuals.infected, out var infected, args.Component))
            return;

        var sprite = new Entity<SpriteComponent>(uid, args.Sprite);

        if (infected)
            SpriteSystem.LayerSetRsiState(sprite.AsNullable(), 0, component.InfectedState);
        else
            SpriteSystem.LayerSetRsiState(sprite.AsNullable(), 0, component.State);
    }
}
