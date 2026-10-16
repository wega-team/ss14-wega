using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Clothing;
using Content.Shared.Armor;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server.Clothing;

public sealed partial class SlimeArmorSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteract(EntityUid uid, SlimeArmorComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || args.Target == uid || !args.CanReach)
            return;

        var target = args.Target.Value;

        if (!TryComp<ClothingComponent>(target, out var clothing))
        {
            _popup.PopupEntity(Loc.GetString("clothing-modifier-armor-not-applicable"), args.User, args.User);
            return;
        }

        var forbiddenSlots = SlotFlags.BACK | SlotFlags.BELT | SlotFlags.SUITSTORAGE | SlotFlags.ANAL | SlotFlags.IDCARD | SlotFlags.EARS | SlotFlags.SOCKS | SlotFlags.UNDERWEARTOP | SlotFlags.UNDERWEARBOTTOM;

        if ((clothing.Slots & forbiddenSlots) != SlotFlags.NONE)
        {
            _popup.PopupEntity(Loc.GetString("clothing-modifier-armor-not-applicable"), args.User, args.User);
            return;
        }

        if (HasComp<ArmorComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("clothing-modifier-armor-not-applicable"), args.User, args.User);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.Delay, new SlimeArmorDoAfterEvent(), uid, target, uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(EntityUid uid, SlimeArmorComponent component, SlimeArmorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var target = args.Target.Value;

		var armor = EnsureComp<ArmorComponent>(target);
		
        if (component.Modifiers != null)
        {
            armor.Modifiers = new DamageModifierSet
            {
                Coefficients = component.Modifiers.Coefficients != null 
                    ? new Dictionary<ProtoId<DamageTypePrototype>, float>(component.Modifiers.Coefficients) 
                    : new Dictionary<ProtoId<DamageTypePrototype>, float>(),
                    
                FlatReductions = component.Modifiers.FlatReductions != null 
                    ? new Dictionary<ProtoId<DamageTypePrototype>, float>(component.Modifiers.FlatReductions) 
                    : new Dictionary<ProtoId<DamageTypePrototype>, float>()
            };
        }
        else
        {
            armor.Modifiers = new DamageModifierSet();
        }

		Dirty(target, armor); 

        _popup.PopupEntity(Loc.GetString("clothing-modifier-armor-success", ("target", target)), args.User, args.User);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Medical/ointment_begin.ogg"), target);

        args.Handled = true;

        if (component.Uses > 0)
        {
            component.Uses--;
            if (component.Uses <= 0)
            {
                QueueDel(uid); 
            }
        }
    }
}
