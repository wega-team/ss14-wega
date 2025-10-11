using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Item; // Corvax-Wega-MagVisuals
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class  BatteryWeaponHitscanModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!; // Corvax-Wega-MagVisuals

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent< BatteryWeaponHitscanModesComponent, UseInHandEvent>(OnUseInHandEvent);
        SubscribeLocalEvent<BatteryWeaponHitscanModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<BatteryWeaponHitscanModesComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, BatteryWeaponHitscanModesComponent component, ExaminedEvent args)
    {
        if (component.FireModes.Count < 2)
            return;

        var fireMode = GetMode(component);
        
		if (!_prototypeManager.TryIndex<HitscanPrototype>(fireMode.Prototype, out var proto))
            return;

        args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", fireMode.Name)));
    }

    private BatteryWeaponHitscanMode GetMode(BatteryWeaponHitscanModesComponent component)
    {
        return component.FireModes[component.CurrentFireMode];
    }

    private void OnGetVerb(EntityUid uid, BatteryWeaponHitscanModesComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (component.FireModes.Count < 2)
            return;

        if (!_accessReaderSystem.IsAllowed(args.User, uid))
            return;
		
		if (TryComp(uid, out HitscanBatteryAmmoProviderComponent? hitscanBatteryAmmoProviderComponent))
			for (var i = 0; i < component.FireModes.Count; i++)
			{
				var fireMode = component.FireModes[i];
				var entProto = _prototypeManager.Index<HitscanPrototype>(fireMode.Prototype);
				var index = i;
	
				var v = new Verb
				{
					Priority = 1,
					Category = VerbCategory.SelectType,
					Text = fireMode.Name,
					Disabled = i == component.CurrentFireMode,
					Impact = LogImpact.Medium,
					DoContactInteraction = true,
					Act = () =>
					{
						TrySetFireMode(uid, component, index, args.User);
					}
				};

				args.Verbs.Add(v);
			}
    }

    private void OnUseInHandEvent(EntityUid uid, BatteryWeaponHitscanModesComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryCycleFireMode(uid, component, args.User);
    }

    public void TryCycleFireMode(EntityUid uid, BatteryWeaponHitscanModesComponent component, EntityUid? user = null)
    {
        if (component.FireModes.Count < 2)
            return;

        var index = (component.CurrentFireMode + 1) % component.FireModes.Count;
        TrySetFireMode(uid, component, index, user);
    }

    public bool TrySetFireMode(EntityUid uid, BatteryWeaponHitscanModesComponent component, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= component.FireModes.Count)
            return false;

        if (user != null && !_accessReaderSystem.IsAllowed(user.Value, uid))
            return false;

        SetFireMode(uid, component, index, user);

        return true;
    }

    private void SetFireMode(EntityUid uid, BatteryWeaponHitscanModesComponent component, int index, EntityUid? user = null)
    {
        var fireMode = component.FireModes[index];
        component.CurrentFireMode = index;
        Dirty(uid, component);
			
		if (_prototypeManager.TryIndex<HitscanPrototype>(fireMode.Prototype, out var prototype))
		{
			// Corvax-Wega-MagVisuals-Edit-start
			if (TryComp<AppearanceComponent>(uid, out var appearance))
			{
				var state = !string.IsNullOrEmpty(fireMode.State)
					? fireMode.State
					: fireMode.Name;

				_appearanceSystem.SetData(uid, BatteryWeaponHitscanModesVisuals.State, state, appearance);

				if (!string.IsNullOrEmpty(fireMode.MagState))
					_appearanceSystem.SetData(uid,BatteryWeaponHitscanModesVisuals.MagState, fireMode.MagState, appearance);
			}

			if (!string.IsNullOrEmpty(fireMode.State))
				_item.SetHeldPrefix(uid, fireMode.State);
			// Corvax-Wega-MagVisuals-Edit-end

			if (user != null)
				_popupSystem.PopupClient(Loc.GetString("gun-set-fire-mode", ("mode", fireMode.Name)), uid, user.Value);
		}
			
		// Corvax-Wega-Weapons-start
        if (TryComp(uid, out HitscanBatteryAmmoProviderComponent? hitscanBatteryAmmoProviderComponent))
        {
            // TODO: Have this get the info directly from the batteryComponent when power is moved to shared.
            var OldFireCost = hitscanBatteryAmmoProviderComponent.FireCost;
            hitscanBatteryAmmoProviderComponent.Prototype = fireMode.Prototype;
            hitscanBatteryAmmoProviderComponent.FireCost = fireMode.FireCost;

            float FireCostDiff = (float)fireMode.FireCost / (float)OldFireCost;
            hitscanBatteryAmmoProviderComponent.Shots = (int)Math.Round(hitscanBatteryAmmoProviderComponent.Shots / FireCostDiff);
            hitscanBatteryAmmoProviderComponent.Capacity = (int)Math.Round(hitscanBatteryAmmoProviderComponent.Capacity / FireCostDiff);

            Dirty(uid, hitscanBatteryAmmoProviderComponent);

            var updateClientAmmoEvent = new UpdateClientAmmoEvent();
            RaiseLocalEvent(uid, ref updateClientAmmoEvent);
		}

    }
}