using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Lavaland.Events;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed partial class FireSelfActionSystem : EntitySystem
{
    [Dependency] private FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FireSelfActionEvent>(OnAction);
    }

    private void OnAction(FireSelfActionEvent args)
    {
        args.Handled = true;
        if (!TryComp<FlammableComponent>(args.Performer, out var flammable))
            return;

        _flammable.AdjustFireStacks(args.Performer, flammable.MaximumFireStacks, ignite: true);
    }
}
