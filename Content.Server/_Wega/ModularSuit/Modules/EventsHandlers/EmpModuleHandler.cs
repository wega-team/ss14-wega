using Content.Shared.Modular.Suit;
using Content.Shared.Emp;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Server.Modular.Suit;

public sealed partial class EMPModuleHandler : ModuleActionHandler
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedEmpSystem _emp = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ModuleEMPEvent>(OnActivate);
    }

    private void OnActivate(Entity<ModularSuitActionHolderComponent> ent, ref ModuleEMPEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindModuleByAction(ent, args.Action, out var moduleEnt))
            return;

        if (!TryComp<ModularSuitModuleComponent>(moduleEnt, out var moduleComp) || !moduleComp.IsActive)
            return;

        var attemptEvent = new ModularSuitModuleAttemptEvent(ent.Owner);
        RaiseLocalEvent(moduleEnt.Value, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return;

        if (PerformEMP(args.Performer))
        {
            ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        }

        args.Handled = true;
    }

    private bool PerformEMP(EntityUid user)
    {
		_emp.EmpPulse(Transform(user).Coordinates, 4f, 75000f, TimeSpan.FromSeconds(8));

        return true;
    }
}
