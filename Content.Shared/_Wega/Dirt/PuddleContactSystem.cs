using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Physics.Events;

namespace Content.Shared.DirtVisuals;

public sealed class PuddleContactSystem : EntitySystem
{
    [Dependency] private readonly SharedDirtSystem _dirt = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DirtSourceComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, DirtSourceComponent comp, ref StartCollideEvent args)
    {
        if (!_solution.TryGetSolution(uid, "puddle", out _, out var puddleSolution))
            return;

        _dirt.ApplyDirtToClothing(args.OtherEntity, puddleSolution);
    }
}
