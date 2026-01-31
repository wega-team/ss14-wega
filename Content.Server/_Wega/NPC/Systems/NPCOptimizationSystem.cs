using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.CCVar;
using Content.Shared.Damage.Systems;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

public sealed class NPCOptimizationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public bool Enabled = true;
    private float _activationRadius = 21f;
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(2.5);
    private TimeSpan _nextUpdate = TimeSpan.Zero;
    private const int MaxProcessPerFrame = 64;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configuration, CCVars.NPCEnabled, value => Enabled = value, true);
        Subs.CVar(_configuration, CCVars.ViewportMaximumWidth, value => _activationRadius = value, true);
        SubscribeLocalEvent<HTNComponent, DamageChangedEvent>(OnDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Enabled)
            return;

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + Cooldown;

        var npcList = new List<(EntityUid, TransformComponent)>();
        var query = EntityQueryEnumerator<HTNComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (_mobState.IsIncapacitated(uid))
                continue;

            if (HasComp<ActorComponent>(uid) || HasComp<NPCIgnoringOptimizeComponent>(uid))
                continue;

            npcList.Add((uid, xform));
        }

        var processedCount = 0;
        _random.Shuffle(npcList); // Random cascading update
        foreach (var (uid, xform) in npcList)
        {
            if (processedCount >= MaxProcessPerFrame)
                break;

            ProcessNPC(uid, xform);
            processedCount++;
        }
    }

    private void OnDamageChanged(EntityUid uid, HTNComponent component, DamageChangedEvent args)
    {
        if (_mobState.IsIncapacitated(uid) || HasComp<ActorComponent>(uid) || args.Origin == null)
            return;

        // Wake the fuck up Samurai, we have a city to burn.
        if (!HasComp<ActiveNPCComponent>(uid))
            _npc.WakeNPC(uid);
    }

    private void ProcessNPC(EntityUid uid, TransformComponent xform)
    {
        var hasNearbyActors = CheckForNearbyActors(xform.Coordinates, _activationRadius);

        var isCurrentlyActive = HasComp<ActiveNPCComponent>(uid);
        if (hasNearbyActors)
        {
            if (!isCurrentlyActive)
                _npc.WakeNPC(uid);
        }
        else
        {
            if (isCurrentlyActive)
                _npc.SleepNPC(uid);
        }
    }

    private bool CheckForNearbyActors(EntityCoordinates coords, float radius)
    {
        var actors = _lookup.GetEntitiesInRange<ActorComponent>(coords, radius);
        foreach (var actor in actors)
        {
            if (!HasComp<GhostComponent>(actor.Owner))
                return true;
        }

        return false;
    }
}
