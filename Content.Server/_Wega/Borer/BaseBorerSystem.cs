using Content.Shared.Borer.BorerInfectedEr;
using Content.Shared.Borer.Wormer;
using Robust.Shared.Containers;
using Robust.Server.Containers;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;

namespace Content.Server.Borer.BorerInfectedSystem;

public sealed class BaseBorerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BorerComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<BorerComponent> ent, ref ComponentInit args)
    {
        _actions.AddAction(ref ent.Owner ent.comp.InfectionActionEntity, ent.comp.id);
    }

}
