using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._Wega.Seraphim;

public abstract partial class SharedSeraphimSystem : EntitySystem
{
    /// Множитель броска клинка
    public const float BladeThrowSpeedMultiplier = 0.25f;

    /// Множитель скорости атаки в Overdrive
    public const float OverdriveAttackRateMultiplier = 1.3f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, AttemptShootEvent>(OnGunShootAttempt);
        SubscribeLocalEvent<HandsComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<MeleeWeaponComponent, GetMeleeAttackRateEvent>(OnOverdriveAttackRate);

        SubscribeLocalEvent<SeraphimOverdriveComponent, RefreshMovementSpeedModifiersEvent>(OnOverdriveSpeed);
        SubscribeLocalEvent<SeraphimExhaustedComponent, RefreshMovementSpeedModifiersEvent>(OnExhaustedSpeed);
        SubscribeLocalEvent<SeraphimSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnSlowedSpeed);
    }

    // Запрет огнестрела
    private void OnGunShootAttempt(Entity<GunComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled || !HasComp<SeraphimImplantComponent>(args.User))
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString("seraphim-cant-shoot");
    }

    // Анти-абуз броском
    private void OnBeforeThrow(Entity<HandsComponent> ent, ref BeforeThrowEvent args)
    {
        if (!HasComp<CorporateBladeComponent>(args.ItemUid))
            return;

        args.ThrowSpeed *= BladeThrowSpeedMultiplier;
    }

    // Ускорение атаки в Overdrive
    private void OnOverdriveAttackRate(Entity<MeleeWeaponComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        if (!HasComp<SeraphimOverdriveComponent>(args.User))
            return;

        args.Multipliers *= OverdriveAttackRateMultiplier;
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
