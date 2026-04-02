using Content.Shared.Hands.EntitySystems;
using Content.Shared.Modular.Suit;
using Robust.Shared.Random;

namespace Content.Server.Modular.Suit;

public sealed class DispenserModuleHandler : ModuleActionHandler
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ActivateDispenserModuleEvent>(OnToggle);
    }

    private void OnToggle(Entity<ModularSuitActionHolderComponent> ent, ref ActivateDispenserModuleEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindModuleByAction(ent, args.Action, out var moduleEnt))
            return;

        if (!TryComp<ModularSuitModuleComponent>(moduleEnt, out var moduleComp) || !moduleComp.IsActive)
            return;

        var item = Spawn(_random.Pick(args.SpawnedProto), Transform(ent).Coordinates);
        _hands.TryPickupAnyHand(args.Performer, item);

        Audio.PlayPvs(args.ActivateSound, ent.Owner);
        ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        args.Handled = true;
    }
}
