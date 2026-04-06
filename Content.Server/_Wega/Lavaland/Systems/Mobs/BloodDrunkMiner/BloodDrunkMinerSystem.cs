using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.SSDIndicator;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class BloodDrunkMinerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodDrunkMinerComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<BloodDrunkMinerComponent, BloodDrunkMinerDashAction>(OnDash);
    }

    // I don't want to add 200+ fields from parents just to remove 1 component from the prototype.
    private void OnMapInit(Entity<BloodDrunkMinerComponent> ent, ref MapInitEvent args)
    {
        RemComp<SSDIndicatorComponent>(ent);
    }

    private void OnDash(Entity<BloodDrunkMinerComponent> ent, ref BloodDrunkMinerDashAction args)
    {
        args.Handled = true;
        _transform.SetCoordinates(ent, args.Target);
        _audio.PlayPvs(args.DashSound, ent);
    }
}
