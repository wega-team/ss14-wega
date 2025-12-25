using Content.Client.DamageState;
using Content.Shared.Mobs;
using Content.Shared.Xenobiology;
using Content.Shared.Xenobiology.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Wega.Xenobiology;

public sealed class SlimeVisualSystem : SharedSlimeVisualSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

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

        _sprite.LayerSetRsiState(ent.Owner, 0, state);
        UpdateDamageVisuals(ent.Owner, stage, type);
    }

    private void UpdateDamageVisuals(EntityUid uid, SlimeStage stage, SlimeType type)
    {
        if (!TryComp<DamageStateVisualsComponent>(uid, out var damageVisuals))
            return;

        var typeStr = type.ToString().ToLower();
        var stageStr = stage == SlimeStage.Young ? "baby" : "adult";

        damageVisuals.States[MobState.Alive] = new()
        {
            [DamageStateVisualLayers.Base] = $"{typeStr}_{stageStr}_slime"
        };

        damageVisuals.States[MobState.Dead] = new()
        {
            [DamageStateVisualLayers.Base] = $"{typeStr}_baby_dead"
        };
    }
}
