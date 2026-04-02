using Content.Server.Temperature.Components;
using Content.Shared.Inventory;
using Content.Shared.Modular.Suit;
using Content.Shared.Temperature;

namespace Content.Server.Modular.Suit;

/// <summary>
/// Use this if you need to transfer effects from clothing hidden under a suit. (Server-side)
/// </summary>
public sealed class ModularSuitHiddenClothingSystem : SharedModularSuitHiddenClothingSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitHiddenClothingComponent, InventoryRelayedEvent<ModifyChangedTemperatureEvent>>(OnModifyChangedTemperature);
    }

    private void OnModifyChangedTemperature(Entity<ModularSuitHiddenClothingComponent> ent, ref InventoryRelayedEvent<ModifyChangedTemperatureEvent> args)
    {
        foreach (var (_, item) in ent.Comp.HiddenItems)
        {
            if (TryComp<TemperatureProtectionComponent>(item, out var tempProtection))
            {
                var coefficient = args.Args.TemperatureDelta < 0
                    ? tempProtection.CoolingCoefficient
                    : tempProtection.HeatingCoefficient;

                var ev = new GetTemperatureProtectionEvent(coefficient);
                RaiseLocalEvent(ent.Owner, ref ev);

                args.Args.TemperatureDelta *= ev.Coefficient;
            }
        }
    }
}
