using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class WieldSpreadModifierSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnGetAmmoSpread(Entity<WieldSpreadModifierComponent> ent, ref GunGetAmmoSpreadEvent args)
    {
        var wielded = TryComp<WieldableComponent>(ent, out var wieldable) && wieldable.Wielded;
        args.Spread *= wielded ? ent.Comp.Wielded : ent.Comp.Unwielded;
    }
}
