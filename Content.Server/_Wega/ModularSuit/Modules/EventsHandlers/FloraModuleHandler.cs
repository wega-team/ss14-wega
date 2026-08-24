using Content.Shared.Modular.Suit;
using Content.Server.Surgery;
using Content.Shared.Surgery;
using Content.Shared.Surgery.Components;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Server.Modular.Suit;

public sealed partial class HealSurgeryModuleHandler : ModuleActionHandler
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SurgerySystem _surgery = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ModuleHealSurgeyEvent>(OnActivate);
    }

    private void OnActivate(Entity<ModularSuitActionHolderComponent> ent, ref ModuleHealSurgeyEvent args)
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

        if (PerformSyrgeyHeal(args.Performer))
        {
            ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        }

        args.Handled = true;
    }

    private bool PerformSyrgeyHeal(EntityUid user)
    {
        if (!TryComp<OperatedComponent>(user, out var comp))
            return false;

        comp.InternalDamages.Clear();

        return true;
    }
}
