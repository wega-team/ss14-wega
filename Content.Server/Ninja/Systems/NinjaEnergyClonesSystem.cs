using Content.Server.NPC.HTN;
using Content.Server.Ninja.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.Ninja.Systems;

public sealed partial class NinjaEnergyClonesSystem : EntitySystem
{
    private const float EnergyClonesCharge = 300f;
    private const int CloneCount = 2;
    private const float TargetScanInterval = 2f;
    private const float ScanRadius = 15f;
    private const string CloneProto = "MobNinjaEnergyClone";
    private const string SuitDisableDelayId = "suit_powers";

    [Dependency] private NinjaCloakSystem _cloak = default!;
    [Dependency] private SpaceNinjaSystem _ninja = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaSuitComponent, NinjaEnergyClonesEvent>(OnEnergyClones);
    }

    // (coords, ninja uid, remaining lifetime, nextAttack to pre-set on child)
    private readonly List<(EntityCoordinates Coords, EntityUid Ninja, float Remaining, TimeSpan NextAttack)> _pendingSpawns = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var meleeQuery = GetEntityQuery<MeleeWeaponComponent>();
        var query = EntityQueryEnumerator<NinjaEnergyCloneComponent, NpcFactionMemberComponent>();
        while (query.MoveNext(out var uid, out var clone, out var faction))
        {
            if (TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState == MobState.Dead)
            {
                QueueDel(uid);
                continue;
            }

            // Detect a completed melee swing: NextAttack advances when the weapon fires.
            if (meleeQuery.TryGetComponent(uid, out var melee) && melee.NextAttack != clone.LastNextAttack)
            {
                clone.LastNextAttack = melee.NextAttack;

                if (_random.Prob(0.5f))
                {
                    float remaining = 1f;
                    if (TryComp<TimedDespawnComponent>(uid, out var timed))
                        remaining = timed.Lifetime;

                    if (remaining >= 0.1f)
                    {
                        var offset = _random.NextAngle().ToVec() * _random.NextFloat(0.5f, 1.5f);
                        _pendingSpawns.Add((Transform(uid).Coordinates.Offset(offset), clone.Ninja, remaining, melee.NextAttack));
                    }
                }
            }

            clone.TargetScanTimer += frameTime;
            if (clone.TargetScanTimer < TargetScanInterval)
                continue;
            clone.TargetScanTimer = 0f;

            EntityUid? mindshieldTarget = null;
            foreach (var target in _faction.GetNearbyHostiles((uid, faction, null), ScanRadius))
            {
                if (HasComp<MindShieldComponent>(target))
                {
                    mindshieldTarget = target;
                    break;
                }
            }

            if (mindshieldTarget != null && TryComp<HTNComponent>(uid, out var htn))
                htn.Blackboard.SetValue("Target", mindshieldTarget.Value);
        }

        var spawnCooldown = _timing.CurTime + TimeSpan.FromSeconds(3);
        foreach (var (coords, ninja, remaining, _) in _pendingSpawns)
        {
            var child = Spawn(CloneProto, coords);
            EnsureComp<TimedDespawnComponent>(child).Lifetime = remaining;
            var childClone = EnsureComp<NinjaEnergyCloneComponent>(child);
            childClone.Ninja = ninja;
            childClone.LastNextAttack = spawnCooldown;
            if (TryComp<MeleeWeaponComponent>(child, out var childMelee))
                childMelee.NextAttack = spawnCooldown;
            EnsureComp<NinjaCloneVisualComponent>(child).NinjaUid = ninja;
            _faction.IgnoreEntity((child, null), (ninja, null));
        }
        _pendingSpawns.Clear();
    }

    private void OnEnergyClones(Entity<NinjaSuitComponent> ent, ref NinjaEnergyClonesEvent args)
    {
        var user = args.Performer;

        if (_cloak.TryRevealCloak(user))
            return;

        args.Handled = true;

        if (TryComp<UseDelayComponent>(ent, out var delay) &&
            _useDelay.IsDelayed((ent, delay), SuitDisableDelayId))
            return;

        if (!_ninja.TryUseCharge(user, EnergyClonesCharge))
            return;

        var userCoords = Transform(user).Coordinates;

        for (var i = 0; i < CloneCount; i++)
        {
            var offset = _random.NextAngle().ToVec() * _random.NextFloat(0.5f, 1.5f);
            var clone = Spawn(CloneProto, userCoords.Offset(offset));
            var cloneComp = EnsureComp<NinjaEnergyCloneComponent>(clone);
            cloneComp.Ninja = user;
            var visual = EnsureComp<NinjaCloneVisualComponent>(clone);
            visual.NinjaUid = user;
            _faction.IgnoreEntity((clone, null), (user, null));
        }
    }
}
