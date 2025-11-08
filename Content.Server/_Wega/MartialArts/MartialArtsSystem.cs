using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Martial.Arts;
using Content.Shared.Martial.Arts.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Martial.Arts;

public sealed partial class MartialArtsSystem : SharedMartialArtsSystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly Dictionary<string, Action<EntityUid>> _blowHandlers;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, MeleeHitEvent>(OnHit);

        // Below is the initialization of subscriptions in styles.
        // Add a field here if you want to add a new martial art style.
        InitializeBoxing();
        InitializeKravMaga();
    }

    private void OnHit(Entity<BodyComponent> ent, ref MeleeHitEvent args)
    {
        if (args.User != args.Weapon || !HasComp<MartialArtsComponent>(args.User)
            || !TryComp<PreparedBlowComponent>(args.User, out var blow))
            return;

        if (blow.SelectedType == null)
            return;

        var target = args.HitEntities.FirstOrNull();
        if (target == null)
            return;

        if (_blowHandlers.TryGetValue(blow.SelectedType, out var handler))
        {
            handler(target.Value);
        }
        else
        {
            var sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("martial_arts");
            sawmill.Error($"Unknown blow type: {blow.SelectedType}");
        }

        RemComp<PreparedBlowComponent>(args.User);
    }

    private void AddPrepared(EntityUid user, string type)
    {
        EnsureComp<PreparedBlowComponent>(user).SelectedType = type;
    }

    private bool TryPrepared(EntityUid user, string type)
    {
        if (TryComp<PreparedBlowComponent>(user, out var prepared))
        {
            prepared.SelectedType = type;
            return true;
        }
        return false;
    }

    /// <summary>
    /// A dictionary for initializing selected types in logic.
    /// Add new fields here if you want to add new types.
    /// </summary>
    private MartialArtsSystem()
    {
        _blowHandlers = new Dictionary<string, Action<EntityUid>>
        {
            /// Boxe
            { "Jab", target => HandleJab(target) },
            { "Hook", target => HandleHook(target) },
            /// KravMaga
            { "LegSweep", target => HandleLegSweep(target) },
            { "NeckChop", target => HandleNeckChop(target) },
            { "LungPunch", target => HandleLungPunch(target) }
        };
    }
}
