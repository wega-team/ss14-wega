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
		
        SubscribeLocalEvent<VeilCultBeaconComponent, ExaminedEvent>(OnExamined);
        // Bound UI subscriptions
        SubscribeLocalEvent<VeilCultBeaconComponent, VeilBeaconNameChangedMessage>(OnVeilBeaconNameChanged);
        SubscribeLocalEvent<VeilCultBeaconComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<VeilCultBeaconComponent, ComponentHandleState>(OnHandleState);
		SubscribeLocalEvent<VeilCultBeaconComponent, ActivateInWorldEvent>(UseVeilBeacon);
    }

    private void OnGetState(Entity<VeilCultBeaconComponent> ent, ref ComponentGetState args)
    {
        args.State = new VeilCultBeaconComponentState(ent.Comp.AssignedLabel)
        {
            MaxLabelChars = ent.Comp.MaxLabelChars,
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

        ent.Comp.MaxLabelChars = state.MaxLabelChars;

        if (ent.Comp.AssignedLabel == state.AssignedLabel)
            return;

        ent.Comp.AssignedLabel = state.AssignedLabel;
        UpdateUI(ent);
    }

    protected virtual void UpdateUI(Entity<VeilCultBeaconComponent> ent)
    {
    }

    private void OnVeilBeaconNameChanged(EntityUid uid, VeilCultBeaconComponent beacon, VeilBeaconNameChangedMessage args)
    {
        var name = args.Name.Trim();
        beacon.AssignedLabel = name[..Math.Min(beacon.MaxLabelChars, name.Length)];
        UpdateUI((uid, beacon));
        Dirty(uid, beacon);

    }

    private void OnExamined(Entity<VeilCultBeaconComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var text = ent.Comp.AssignedLabel == string.Empty
            ? Loc.GetString("hand-labeler-examine-blank")
            : Loc.GetString("hand-labeler-examine-label-text", ("label-text", ent.Comp.AssignedLabel));
        args.PushMarkup(text);
    }
}