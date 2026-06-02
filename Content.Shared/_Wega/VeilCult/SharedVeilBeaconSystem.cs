using Content.Shared.Interaction;
using Content.Shared.Veil.Cult.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Veil.Cult;

public sealed partial class VeilBeaconSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

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
        => args.State = new VeilCultBeaconComponentState(ent.Comp.AssignedName, ent.Comp.MaxNameChars);

    private void UseVeilBeacon(EntityUid uid, VeilCultBeaconComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<VeilCultistComponent>(args.User) && !HasComp<VeilCultConstructComponent>(args.User))
            return;

        _ui.OpenUi(uid, VeilBeaconUiKey.Key, args.User);
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

    private void OnVeilBeaconNameChanged(EntityUid uid, VeilCultBeaconComponent beacon, VeilBeaconNameChangedMessage args)
    {
        var name = args.Name.Trim();
        if (name.Length > 0)
            beacon.AssignedName = name[..Math.Min(beacon.MaxNameChars, name.Length)];
        else
            beacon.AssignedName = Loc.GetString("veil-cult-unknown-beacon");

        Dirty(uid, beacon);
        UpdateUI((uid, beacon));
    }

    private void UpdateUI(Entity<VeilCultBeaconComponent> ent)
    {
        if (_ui.HasUi(ent, VeilBeaconUiKey.Key))
        {
            var state = new VeilBeaconNameBoundUserInterfaceState(
                ent.Comp.AssignedName, ent.Comp.MaxNameChars);

            _ui.SetUiState(ent.Owner, VeilBeaconUiKey.Key, state);
        }
    }
}
