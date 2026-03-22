using Content.Shared.Modular.Suit;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitModuleContainerRequirementSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitModuleContainerRequirementComponent, ModularSuitModuleAttemptEvent>(OnAttemptActivate);
    }

    private void OnAttemptActivate(Entity<ModularSuitModuleContainerRequirementComponent> ent, ref ModularSuitModuleAttemptEvent args)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.RequiredContainerId, out var container))
        {
            _popup.PopupPredicted(Loc.GetString(ent.Comp.FailureMessage), args.Suit, null);
            args.Cancel();
            return;
        }

        if (container.ContainedEntities.Count == 0)
        {
            _popup.PopupPredicted(Loc.GetString(ent.Comp.FailureMessage), args.Suit, null);
            args.Cancel();
        }
    }
}
