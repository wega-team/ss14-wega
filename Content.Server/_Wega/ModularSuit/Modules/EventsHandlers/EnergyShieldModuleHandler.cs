using Content.Shared.EnergyShield;
using Content.Shared.Modular.Suit;

namespace Content.Server.Modular.Suit;

public sealed class EnergyShieldModuleHandler : ModuleActionHandler
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ActivateEnergyShieldModuleEvent>(OnToggle);
    }

    private void OnToggle(Entity<ModularSuitActionHolderComponent> ent, ref ActivateEnergyShieldModuleEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindModuleByAction(ent, args.Action, out var moduleEnt))
            return;

        if (!TryComp<ModularSuitModuleComponent>(moduleEnt, out var moduleComp) || !moduleComp.IsActive)
            return;

        var user = args.Performer;
        var shield = EnsureComp<EnergyShieldOwnerComponent>(user);
        shield.ShieldEntity = Spawn(args.ShieldProto, Transform(user).Coordinates);
        shield.SustainingCount = 5;

        _transform.SetParent(shield.ShieldEntity.Value, user);
        ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        args.Handled = true;
    }
}
