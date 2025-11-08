using System.Linq;
using Content.Shared.RCD.Components;
using Content.Shared.RCD;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.RCD.Systems;

public sealed partial class RCDUpgradeKitSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RCDUpgradeKitComponent, AfterInteractEvent>(OnAfterInteract);
    }


    private void OnAfterInteract(Entity<RCDUpgradeKitComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (!TryComp<RCDComponent>(args.Target, out var rcd) || rcd.Reinforced)
            return;

        _audio.PlayPredicted(ent.Comp.UpgradeSound, args.Target.Value, args.User);

        rcd.Reinforced = true;
        QueueDel(ent);

        args.Handled = true;
    }

}
