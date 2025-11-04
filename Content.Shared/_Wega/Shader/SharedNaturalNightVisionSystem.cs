using Content.Shared.Actions;

namespace Content.Shared.Shaders;

public sealed class SharedNaturalNightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NaturalNightVisionComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NaturalNightVisionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Action = _action.AddAction(ent, ent.Comp.ActionProto);
        Dirty(ent.Owner, ent.Comp);
    }
}