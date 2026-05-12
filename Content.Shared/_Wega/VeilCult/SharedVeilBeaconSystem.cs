using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.GameObjects;

namespace Content.Shared.Veil.Cult;

public abstract class SharedVeilBeaconSystem : EntitySystem
{
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;
	
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();
		
        // Bound UI subscriptions
        SubscribeLocalEvent<VeilCultBeaconComponent, VeilBeaconNameChangedMessage>(OnVeilBeaconNameChanged);
        SubscribeLocalEvent<VeilCultBeaconComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<VeilCultBeaconComponent, ComponentHandleState>(OnHandleState);
		SubscribeLocalEvent<VeilCultBeaconComponent, ActivateInWorldEvent>(UseVeilBeacon);
    }

    private void OnGetState(Entity<VeilCultBeaconComponent> ent, ref ComponentGetState args)
    {
        args.State = new VeilCultBeaconComponentState(ent.Comp.AssignedName)
        {
            MaxNameChars = ent.Comp.MaxNameChars,
        };
    }

    private void UseVeilBeacon(EntityUid uid, VeilCultBeaconComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;
        
        if (!HasComp<VeilCultistComponent>(args.User) && !HasComp<VeilCultConstructComponent>(args.User))
            return;

        UserInterfaceSystem.OpenUi(uid, VeilBeaconUiKey.Key, args.User);
        args.Handled = true;
    }

    private void OnHandleState(Entity<VeilCultBeaconComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not VeilCultBeaconComponentState state)
            return;

        ent.Comp.MaxNameChars = state.MaxNameChars;

        if (ent.Comp.AssignedName == state.AssignedName)
            return;

        ent.Comp.MaxNameChars = state.MaxNameChars;
        UpdateUI(ent);
    }

    protected virtual void UpdateUI(Entity<VeilCultBeaconComponent> ent)
    {
    }

    private void OnVeilBeaconNameChanged(EntityUid uid, VeilCultBeaconComponent beacon, VeilBeaconNameChangedMessage args)
    {
        var name = args.Name.Trim();
		if (name.Length > 0)
			beacon.AssignedName = name[..Math.Min(beacon.MaxNameChars, name.Length)];
		else
			beacon.AssignedName = Loc.GetString("veil-cult-unknown-beacon");
        UpdateUI((uid, beacon));
        Dirty(uid, beacon);

    }

}