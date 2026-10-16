using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Clothing;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Clothing;

public sealed class SlimeSpeedSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeSpeedComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SlimeSpeedComponent, SlimeSpeedDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, SlimeSpeedComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || args.Target == uid || !args.CanReach)
            return;

        var target = args.Target.Value;

        if (!HasComp<ClothingSpeedModifierComponent>(target) && !HasComp<SpeedModifiedOnWieldComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("clothing-modifier-remover-not-applicable"), args.User, args.User);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.Delay, new SlimeSpeedDoAfterEvent(), uid, target, uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, SlimeSpeedComponent component, SlimeSpeedDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!TryComp<ClothingSpeedModifierComponent>(target, out var speedMod) && !TryComp<SpeedModifiedOnWieldComponent>(target, out var speedWield))
        {
            _popup.PopupEntity(Loc.GetString("clothing-modifier-remover-not-applicable"), args.User, args.User);
            return;
        }

        if (TryComp<ClothingSpeedModifierComponent>(target, out var speedMod1))
			RemComp<ClothingSpeedModifierComponent>(target);

        if (TryComp<SpeedModifiedOnWieldComponent>(target, out var speedWield1))
			RemComp<SpeedModifiedOnWieldComponent>(target);

        _popup.PopupEntity(Loc.GetString("clothing-modifier-remover-success", ("target", target)), args.User, args.User);
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
