using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Robust.Shared.Collections;

namespace Content.Server.Ninja.Systems;

public sealed partial class NinjaJetpackSystem : SharedNinjaJetpackSystem
{
    [Dependency] private SpaceNinjaSystem _ninja = default!;

    protected override bool CanEnable(EntityUid uid, NinjaJetpackComponent comp)
    {
        if (!base.CanEnable(uid, comp))
            return false;

        if (!Container.TryGetContainingContainer((uid, null, null), out var container))
            return false;

        return _ninja.TryUseCharge(container.Owner, 0f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toDisable = new ValueList<(EntityUid Uid, NinjaJetpackComponent Comp)>();
        var query = EntityQueryEnumerator<ActiveNinjaJetpackComponent, NinjaJetpackComponent>();

        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            if (comp.JetpackUser == null)
            {
                toDisable.Add((uid, comp));
                continue;
            }

            active.ChargeAccumulator += frameTime;
            if (active.ChargeAccumulator < 1f)
                continue;

            active.ChargeAccumulator -= 1f;

            if (!_ninja.TryUseCharge(comp.JetpackUser.Value, comp.ChargePerSecond))
                toDisable.Add((uid, comp));
        }

        foreach (var (uid, comp) in toDisable)
            SetEnabled(uid, comp, false);
    }
}
