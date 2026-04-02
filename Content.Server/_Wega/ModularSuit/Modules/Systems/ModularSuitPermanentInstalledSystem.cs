using Content.Shared.Modular.Suit;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitPermanentInstalledSystem : EntitySystem
{
    [Dependency] private readonly SharedModularSuitSystem _modularSuit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitPermanentInstalledComponent, ModularSuitInstalledEvent>(OnModuleInstalled);
    }

    private void OnModuleInstalled(Entity<ModularSuitPermanentInstalledComponent> module, ref ModularSuitInstalledEvent args)
    {
        _modularSuit.SetModulePermanent(module.Owner, true);
    }
}
