using Content.Shared.Damage;
using Content.Shared.Martial.Arts;
using Content.Shared.Martial.Arts.Components;
using Content.Shared.Standing;

namespace Content.Server.Martial.Arts;

/// KravMaga
public sealed partial class MartialArtsSystem
{
    private void InitializeKravMaga()
    {
        SubscribeLocalEvent<MartialArtsComponent, KravMagaLegSweepActionEvent>(OnLegSweep);
        SubscribeLocalEvent<MartialArtsComponent, KravMagaNeckChopActionEvent>(OnNeckChop);
        SubscribeLocalEvent<MartialArtsComponent, KravMagaLungPunchActionEvent>(OnLungPunch);
    }

    #region Processing
    private void OnLegSweep(Entity<MartialArtsComponent> ent, ref KravMagaLegSweepActionEvent args)
    {
        args.Handled = true;
        string type = "LegSweep";
        if (TryPrepared(ent, type))
            return;

        AddPrepared(ent, type);
    }

    private void OnNeckChop(Entity<MartialArtsComponent> ent, ref KravMagaNeckChopActionEvent args)
    {
        args.Handled = true;
        string type = "NeckChop";
        if (TryPrepared(ent, type))
            return;

        AddPrepared(ent, type);
    }

    private void OnLungPunch(Entity<MartialArtsComponent> ent, ref KravMagaLungPunchActionEvent args)
    {
        args.Handled = true;
        string type = "LungPunch";
        if (TryPrepared(ent, type))
            return;

        AddPrepared(ent, type);
    }
    #endregion

    #region Handles
    private void HandleLegSweep(EntityUid target)
    {
        if (TryComp(target, out StandingStateComponent? standing) && !standing.Standing)
            return;

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(4f), true);
    }

    private void HandleNeckChop(EntityUid target)
    {
        _statusEffect.TryAddStatusEffectDuration(target, "Muted", TimeSpan.FromSeconds(20f));

        var damage = new DamageSpecifier { DamageDict = { { "Blunt", 15 } } };
        _damage.TryChangeDamage(target, damage);
    }

    private void HandleLungPunch(EntityUid target)
    {
        var damage = new DamageSpecifier { DamageDict = { { "Blunt", 15 }, { "Asphyxiation", 10 } } };
        _damage.TryChangeDamage(target, damage);
    }
    #endregion
}
