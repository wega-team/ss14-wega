using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.UserInterface;
using Content.Shared.Whitelist;
using Content.Shared.Teleportation.Components;

namespace Content.Server.Veil.Cult;


public sealed partial class TeleportionEnchantSystem : SharedTeleportationEnchantSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportationEnchantComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TeleportationEnchantComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);
    }

    private void OnMapInit(Entity<TeleportationEnchantComponent> ent, ref MapInitEvent args)
    {
        UpdateTeleportPoints(ent);
    }

    private void OnBeforeUiOpen(Entity<TeleportationEnchantComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateTeleportPoints(ent);
    }

    private void UpdateTeleportPoints(Entity<TeleportationEnchantComponent> ent)
    {
        ent.Comp.AvailableWarps.Clear();

        var allEnts = AllEntityQuery<VeilCultBeaconComponent>();

        while (allEnts.MoveNext(out var warpEnt, out var warpPointComp))
            ent.Comp.AvailableWarps.Add(new TeleportPoint(warpPointComp.AssignedLabel, GetNetEntity(warpEnt)));

        Dirty(ent);
    }
}