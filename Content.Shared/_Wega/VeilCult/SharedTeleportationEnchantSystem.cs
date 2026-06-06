using Content.Shared.Veil.Cult.Components;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;

namespace Content.Shared.Veil.Cult;

public abstract partial class SharedTeleportationEnchantSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportationEnchantComponent, TeleportEnchantDestinationMessage>(OnTeleportToLocationRequest);
        SubscribeLocalEvent<VeilCultistComponent, VeilCultTeleportDoAfterEvent>(OnTeleportSuccess);
    }

    private void OnTeleportToLocationRequest(Entity<TeleportationEnchantComponent> ent, ref TeleportEnchantDestinationMessage args)
    {
        if (!TryGetEntity(args.NetEnt, out var telePointEnt) || TerminatingOrDeleted(telePointEnt) || !HasComp<VeilCultBeaconComponent>(telePointEnt))
            return;

        Teleport(args.Actor, args.NetEnt, ent.Owner);
        _ui.CloseUis(ent.Owner);
    }

    private void Teleport(EntityUid user, NetEntity beacon, EntityUid used)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(4),
            new VeilCultTeleportDoAfterEvent(beacon), user, used)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = false
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnTeleportSuccess(EntityUid uid, VeilCultistComponent comp, VeilCultTeleportDoAfterEvent args)
    {
        if (args.Used == null || args.Cancelled || args.Handled)
            return;

        var beacon = GetEntity(args.Beacon);
        Spawn("BloodCultOutEffect", Transform(uid).Coordinates);
        _transform.SetCoordinates(uid, Transform(beacon).Coordinates);

        RemComp<EnchantedComponent>(args.Used.Value);
        RemComp<TeleportationEnchantComponent>(args.Used.Value);
        RemComp<ActivatableUIComponent>(args.Used.Value);
        args.Handled = true;
    }
}
