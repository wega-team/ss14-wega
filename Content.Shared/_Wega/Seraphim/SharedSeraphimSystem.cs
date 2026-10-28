using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._Wega.Seraphim;

public abstract partial class SharedSeraphimSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeraphimImplantComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<SeraphimImplantComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<CorporateBladeComponent, GetMeleeAttackRateEvent>(OnOverdriveAttackRate);

        SubscribeLocalEvent<SeraphimOverdriveComponent, RefreshMovementSpeedModifiersEvent>(OnOverdriveSpeed);
        SubscribeLocalEvent<SeraphimExhaustedComponent, RefreshMovementSpeedModifiersEvent>(OnExhaustedSpeed);
        SubscribeLocalEvent<SeraphimSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnSlowedSpeed);
    }

    // Запрет огнестрела
    private void OnShootAttempt(Entity<SeraphimImplantComponent> ent, ref ShotAttemptedEvent args)
    {
        args.Cancel();
        _popup.PopupEntity(Loc.GetString("seraphim-cant-shoot"), ent, ent);
    }

    // Анти-абуз броском
    private void OnBeforeThrow(Entity<SeraphimImplantComponent> ent, ref BeforeThrowEvent args)
    {
        if (!TryComp<CorporateBladeComponent>(args.ItemUid, out var blade))
            return;

        args.ThrowSpeed *= blade.ThrowSpeedMultiplier;
    }

    // Ускорение атаки в Overdrive
    private void OnOverdriveAttackRate(Entity<CorporateBladeComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        if (!TryComp<SeraphimOverdriveComponent>(args.User, out var overdrive))
            return;

        args.Multipliers *= overdrive.AttackRateMultiplier;
    }

    private void OnOverdriveSpeed(Entity<SeraphimOverdriveComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedMultiplier);
    }

    private void OnExhaustedSpeed(Entity<SeraphimExhaustedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedMultiplier);
    }

    private void OnSlowedSpeed(Entity<SeraphimSlowedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedMultiplier);
    }

    /// <summary>
    /// Активна ли сейчас какая-либо способность импланта.
    /// </summary>
    public bool IsAbilityActive(EntityUid uid)
    {
        return HasComp<SeraphimPreparedComponent>(uid)
            || HasComp<SeraphimConcentratingComponent>(uid)
            || HasComp<SeraphimOverdriveComponent>(uid);
    }
}
