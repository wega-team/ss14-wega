using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Veil.Cult.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class VeilCultBeaconObjectiveSystem : EntitySystem
{
    [Dependency] private TargetObjectiveSystem _target = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VeilCultBeaconObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, VeilCultBeaconObjectiveComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target) || target == null)
            return;

        var targetUid = target.Value;

        var nearbyEntity = _entityLookup.GetEntitiesInRange<VeilCultBeaconComponent>(Transform(targetUid).Coordinates, 10f);
        if (nearbyEntity.Count > 0)
        {
            args.Progress = 1f;
            return;
        }

        args.Progress = 0f;
    }
}
