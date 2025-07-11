using Content.Shared.Xenobiology.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology;

public abstract class SharedSlimeVisualSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeVisualsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlimeVisualsComponent, SlimeTypeChangedEvent>(OnTypeChanged);
        SubscribeLocalEvent<SlimeVisualsComponent, SlimeStageChangedEvent>(OnStageChanged);
    }

    private void OnMapInit(EntityUid uid, SlimeVisualsComponent component, MapInitEvent args)
    {
        UpdateVisuals(uid, component);
    }

    private void OnTypeChanged(EntityUid uid, SlimeVisualsComponent component, ref SlimeTypeChangedEvent args)
    {
        UpdateVisuals(uid, component);
    }

    private void OnStageChanged(EntityUid uid, SlimeVisualsComponent component, ref SlimeStageChangedEvent args)
    {
        UpdateVisuals(uid, component);
    }

    protected void UpdateVisuals(EntityUid uid, SlimeVisualsComponent component)
    {
        if (!TryComp<SlimeGrowthComponent>(uid, out var growth))
            return;

        var protoId = component.TypeVisuals.TryGetValue(growth.SlimeType, out var typeProto)
            ? typeProto
            : component.DefaultVisuals;

        if (protoId == null || !_proto.TryIndex(protoId, out var proto))
            return;

        _metaData.SetEntityName(uid, proto.Name);
        _metaData.SetEntityDescription(uid, proto.Description);

        if (TryComp<AppearanceComponent>(uid, out var appearance) &&
            proto.TryGetComponent("Appearance", out AppearanceComponent? appearanceOther))
        {
            _appearance.SetData(uid, SlimeVisualLayers.Type, growth.SlimeType, appearance);
            _appearance.SetData(uid, SlimeVisualLayers.Stage, growth.CurrentStage, appearance);
            _appearance.AppendData(appearanceOther, uid);
            Dirty(uid, appearance);
        }
    }
}
