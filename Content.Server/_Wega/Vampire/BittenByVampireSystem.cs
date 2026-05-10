using Content.Shared.HealthExaminable;
using Content.Shared.Surgery.Components;
using Content.Shared.Vampire.Components;
using Robust.Shared.Timing;

namespace Content.Server.Vampire;

/// <summary>
/// Medics can see bite on your neck.
/// </summary>
public sealed class BittenByVampireSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BittenByVampireComponent, ComponentAdd>(OnComponentAdd);
        SubscribeLocalEvent<BittenByVampireComponent, HealthBeingExaminedEvent>(OnHealthBeingExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _timing.CurTime;

        var query = EntityQueryEnumerator<BittenByVampireComponent>();
        while (query.MoveNext(out var uid, out var bitten))
        {
            if (currentTime >= bitten.ExpirationTime)
                RemComp<BittenByVampireComponent>(uid);
        }
    }

    private void OnComponentAdd(EntityUid uid, BittenByVampireComponent component, ComponentAdd args)
    {
        component.ExpirationTime = _timing.CurTime + TimeSpan.FromSeconds(component.LifetimeSeconds);
    }

    private void OnHealthBeingExamined(Entity<BittenByVampireComponent> ent, ref HealthBeingExaminedEvent args)
    {
        if (HasComp<SurgicalSkillComponent>(args.Examiner))
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("vampire-bittenbyvampire-examine"));
        }
    }
}
