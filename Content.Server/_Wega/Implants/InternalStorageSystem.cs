using Content.Server.Lavaland.Mobs;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Lavaland.Components;
using Content.Shared.Mobs;

namespace Content.Server.Implants;

public sealed class InternalStorageSystem : SharedInternalStorageSystem
{
    [Dependency] private readonly LegionSystem _legion = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InternalStorageComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<InternalStorageComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.OldMobState != MobState.Alive || args.NewMobState == MobState.Dead)
            return;

        foreach (var contained in ent.Comp.BodyContainer.ContainedEntities)
        {
            if (TryComp<LegionCoreComponent>(contained, out var legionCore) && legionCore.Active)
            {
                _legion.PerformCoreHeal(ent.Owner, (contained, legionCore));
                break;
            }
        }
    }
}
