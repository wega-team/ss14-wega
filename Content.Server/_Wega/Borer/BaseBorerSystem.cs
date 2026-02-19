using Content.Shared.Borer.BorerInfectedEr;
using Content.Shared.Borer.Wormer;
using Robust.Shared.Containers;
using Robust.Server.Containers;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Server.Popups;

namespace Content.Server.Borer.BorerInfectedSystem;

public sealed class BaseBorerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BorerComponent, InfectionBorerEvent>(OnInfectionUse);
    }

    private void OnInfectionUse(Entity<BorerComponent> ent, ref ComponentInit args)
    {
         _popupSystem.
        if 
    }
}