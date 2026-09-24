using Content.Shared.Modular.Suit;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Server.Modular.Suit;

public sealed partial class StealthModuleHandler : ModuleActionHandler
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ModuleStealthEvent>(OnActivate);
    }

    private void OnActivate(Entity<ModularSuitActionHolderComponent> ent, ref ModuleStealthEvent args)
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

        if (PerformStealth(args.Performer, args.Coefficient))
        {
            ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        }

        args.Handled = true;
    }

    private bool PerformStealth(EntityUid user, float strong)
    {
        if (!TryComp<StealthComponent>(user, out var stealth))
        {
            stealth = EnsureComp<StealthComponent>(user);
            _stealth.SetVisibility(user, strong, stealth);
            _stealth.SetEnabled(user, false, stealth);
        }

        if (stealth.Enabled)
        {
            _stealth.SetEnabled(user, false, stealth);
        }
        else
        {
            _stealth.SetEnabled(user, true, stealth);
        }

        return true;
    }
}
