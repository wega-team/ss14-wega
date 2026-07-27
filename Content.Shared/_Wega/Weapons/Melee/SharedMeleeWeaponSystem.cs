using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using ItemToggleMeleeWeaponComponent = Content.Shared.Item.ItemToggle.Components.ItemToggleMeleeWeaponComponent;
using Content.Shared.Examine; // Corvax-Wega-Add

namespace Content.Shared.Weapons.Melee;

public abstract partial class SharedMeleeWeaponSystem : EntitySystem
{
	[SubscribeLocalEvent]
     private void OnExamine(EntityUid uid, BonusMeleeDamageComponent component, ExaminedEvent args)
    {
        if (component.HeavyDamageMultiplier == 1)
            return;

		var myFloat = component.HeavyDamageMultiplier;
		string booster = myFloat.ToString();
		
        args.PushMarkup(Loc.GetString("damage-booster",
                ("booster", booster)));
    }
}
