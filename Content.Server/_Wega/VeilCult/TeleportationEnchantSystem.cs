using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Teleportation.Components;

namespace Content.Server.Veil.Cult;


public sealed partial class TeleportionEnchantSystem : SharedTeleportationEnchantSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportationEnchantComponent, BoundUIOpenedEvent>(OnUiOpen);
    }

    private void OnUiOpen(Entity<TeleportationEnchantComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateTeleportPoints(ent);
    }

    private void UpdateTeleportPoints(Entity<TeleportationEnchantComponent> ent)
    {
        ent.Comp.AvailableWarps.Clear();

        var allEnts = AllEntityQuery<VeilCultBeaconComponent>();

        while (allEnts.MoveNext(out var warpEnt, out var warpPointComp))
            ent.Comp.AvailableWarps.Add(new TeleportPoint(warpPointComp.AssignedName, GetNetEntity(warpEnt)));

        _ui.SetUiState(ent.Owner, TeleportEnchantUiKey.Key, new TeleportationEnchantBoundUserInterfaceState(ent.Comp.AvailableWarps));
        Dirty(ent);
    }
}
