using Content.Shared.Veil.Cult.Components;
using Content.Shared.Timing;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;

namespace Content.Shared.Veil.Cult;

public abstract partial class SharedTeleportationEnchantSystem : EntitySystem
{	
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    protected const string TeleportDelay = "TeleportDelay";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportationEnchantComponent, TeleportEnchantDestinationMessage>(OnTeleportToLocationRequest);
		SubscribeLocalEvent<VeilCultistComponent, VeilCultTeleportDoAfterEvent>(OnTeleportSuccess);
    }

    protected virtual void OnTeleportToLocationRequest(Entity<TeleportationEnchantComponent> ent, ref TeleportEnchantDestinationMessage args)
    {
        if (!TryGetEntity(args.NetEnt, out var telePointEnt) || TerminatingOrDeleted(telePointEnt) || !HasComp<VeilCultBeaconComponent>(telePointEnt) || args.Actor == null)
            return;

        Teleport(args.Actor, args.NetEnt, ent.Owner);

		if (TryComp<UserInterfaceComponent>(ent.Owner, out var ui))
			_ui.CloseUi((ent.Owner, ui), TeleportEnchantUiKey.Key);

    }
	
    private void Teleport(EntityUid user, NetEntity beacon, EntityUid used)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(4),
            new VeilCultTeleportDoAfterEvent() { Target = beacon },
            eventTarget: user,
			used: used
			)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = false
        };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }
	
    private void OnTeleportSuccess(EntityUid uid, VeilCultistComponent comp, VeilCultTeleportDoAfterEvent args)
    {   
		if (args.Target == null || args.Used == null || args.Cancelled || args.Handled)
			return;
		
		var beacon = GetEntity(args.Target);
		Spawn("BloodCultOutEffect", Transform(uid).Coordinates);
        _transform.SetCoordinates(uid, Transform(beacon).Coordinates);
		
        RemComp<EnchantedComponent>(args.Used.Value);
        RemComp<TeleportationEnchantComponent>(args.Used.Value);
		RemComp<ActivatableUIComponent>(args.Used.Value);
		args.Handled = true;
    }
}