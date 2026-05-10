using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.NullRod.Components;
using Content.Shared.Vampire.Components;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    private void InitializeThralls()
    {
        SubscribeLocalEvent<ThrallOwnerComponent, DamageChangedEvent>(OnVampireDamageChanged);
        SubscribeLocalEvent<ThrallComponent, DamageChangedEvent>(OnThrallDamageChanged);
        SubscribeLocalEvent<MindShieldComponent, ComponentStartup>(MindShieldImplanted); // TODO: Replace this with a specific event
    }

    #region Damage Sharing Logic

    private void OnVampireDamageChanged(EntityUid uid, ThrallOwnerComponent component, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is null || !args.DamageIncreased)
            return;

        if (!TryComp<ThrallOwnerComponent>(uid, out var thrallOwner) || !thrallOwner.DamageSharing)
            return;

        var aliveThralls = GetAliveThralls(thrallOwner);
        if (aliveThralls.Count == 0)
            return;

        _damage.TryChangeDamage(uid, -args.DamageDelta, true, false);
        DistributeDamage(uid, args.DamageDelta, aliveThralls);
    }

    private void OnThrallDamageChanged(EntityUid uid, ThrallComponent component, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is null || !args.DamageIncreased)
            return;

        if (component.VampireOwner is not { } vampireOwner)
            return;

        if (!TryComp<ThrallOwnerComponent>(vampireOwner, out var thrallOwner) || !thrallOwner.DamageSharing)
            return;

        var aliveThralls = GetAliveThralls(thrallOwner);
        if (aliveThralls.Count == 0)
            return;

        _damage.TryChangeDamage(uid, -args.DamageDelta, true, false);
        DistributeDamage(vampireOwner, args.DamageDelta, aliveThralls);
    }

    private void DistributeDamage(EntityUid vampireUid, DamageSpecifier originalDamage, List<EntityUid> aliveThralls)
    {
        var participants = new List<EntityUid> { vampireUid };
        participants.AddRange(aliveThralls);

        if (participants.Count == 0)
            return;

        var sharedDamage = GetSharedDamage(originalDamage, participants.Count);
        foreach (var participant in participants)
        {
            if (!HasComp<DamageableComponent>(participant))
                continue;

            _damage.TryChangeDamage(participant, sharedDamage, true, false);
        }
    }

    #endregion

    private void MindShieldImplanted(EntityUid uid, MindShieldComponent comp, ComponentStartup init)
    {
        if (!TryComp<ThrallComponent>(uid, out var thrall) || thrall.VampireOwner is not { } owner)
            return;

        var stunTime = TimeSpan.FromSeconds(4);
        var name = Identity.Entity(uid, EntityManager);

        if (TryComp<ThrallOwnerComponent>(owner, out var thrallOwner))
        {
            TryRemoveThrall(thrallOwner, uid);
            Dirty(owner, thrallOwner);
        }

        RemComp<ThrallComponent>(uid);
        RemComp<UnholyComponent>(uid);
        RemComp<NullDamageComponent>(uid);
        _stun.TryUpdateParalyzeDuration(uid, stunTime);
        _popup.PopupEntity(Loc.GetString("thrall-break-control", ("name", name)), uid);
    }
}
