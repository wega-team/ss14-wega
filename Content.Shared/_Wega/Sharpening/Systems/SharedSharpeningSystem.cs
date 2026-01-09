using Content.Shared.Examine;
using Content.Shared.Sharpening.Components;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Sharpening.Events;

namespace Content.Shared.Sharpening.Systems;

public abstract class SharedSharpeningSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpeningComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SharpeningComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SharpeningComponent, MeleeHitEvent>(OnMeleeHitEvent);
    }

    private void OnExamined(EntityUid uid, SharpeningComponent comp, ExaminedEvent args)
    {
        if (!comp.Sharpened)
            return;

        args.PushMarkup(Loc.GetString("sharpering-remaining-hits", ("hits", comp.UsesRemaining)));
    }

    private void OnAfterInteract(Entity<SharpeningComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (!TryComp<SharpenerComponent>(args.Target, out var sharpener))
            return;

        if (ent.Comp.Sharpened)
            return;

        if (!TryComp<MeleeWeaponComponent>(ent, out var weapon))
            return;

        if (ent.Comp.Difficulty > sharpener.Strength || sharpener.UsesMultiplier <= 0)
            return;

        _audio.PlayPredicted(sharpener.SharpeningSound, args.Target.Value, args.User);

        ent.Comp.Sharpened = true;
        ent.Comp.UsesRemaining = (int)(ent.Comp.Uses * sharpener.UsesMultiplier);
        ent.Comp.PreviousDamage = weapon.Damage;
        weapon.Damage = ent.Comp.Damage;

        var ev = new SharpeningFinishedEvent(GetNetEntity(args.Target.Value), GetNetEntity(ent.Owner));
        RaiseLocalEvent(args.Target.Value, ev);

        args.Handled = true;
    }

    private void OnMeleeHitEvent(Entity<SharpeningComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (!ent.Comp.Sharpened)
            return;

        if (!TryComp<MeleeWeaponComponent>(ent, out var weapon))
            return;

        if (ent.Comp.UsesRemaining < 2)
        {
            ent.Comp.Sharpened = false;
            weapon.Damage = ent.Comp.PreviousDamage;
        }
        else
        {
            ent.Comp.UsesRemaining -= 1;
        }
    }
}
