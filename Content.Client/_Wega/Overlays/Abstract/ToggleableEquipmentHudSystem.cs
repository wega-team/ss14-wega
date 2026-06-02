using Content.Shared.Overlays;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;

namespace Content.Client.Overlays;

public abstract partial class ToggleableEquipmentHudSystem<T> : EquipmentHudSystem<T>
    where T : ToggleableHudComponent
{
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnRefreshComponentHud(Entity<T> ent, ref RefreshEquipmentHudEvent<T> args)
    {
        if (!ent.Comp.Enabled)
            return;

        base.OnRefreshComponentHud(ent, ref args);
    }
}
