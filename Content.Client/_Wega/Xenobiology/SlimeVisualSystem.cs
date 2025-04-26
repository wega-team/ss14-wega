using Content.Shared.Xenobiology;
using Content.Shared.Xenobiology.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Wega.Xenobiology;

public sealed class SlimeVisualSystem : SharedSlimeVisualSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<SlimeVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<SlimeType>(ent, SlimeVisualLayers.Type, out var type, args.Component) ||
            !_appearance.TryGetData<SlimeStage>(ent, SlimeVisualLayers.Stage, out var stage, args.Component))
            return;

        var state = stage == SlimeStage.Young
            ? $"{type.ToString().ToLower()}_baby_slime"
            : $"{type.ToString().ToLower()}_adult_slime";

        args.Sprite.LayerSetState(0, state);
    }
}
