namespace Content.Shared.Modular.Suit;

[ByRefEvent]
public sealed class ModularSuitDeployAttemptEvent : CancellableEntityEventArgs;

[ByRefEvent]
public readonly record struct ModularSuitRefreshPowerEvent();

[ByRefEvent]
public readonly record struct ModularSuitChargeChangedEvent(float NewCharge, float MaxCharge);

[ByRefEvent]
public readonly record struct ModularSuitActiveChangedEvent(EntityUid Suit, bool Active, EntityUid? Wearer);

[ByRefEvent]
public readonly record struct ModularSuitInstalledEvent(EntityUid Suit, EntityUid? User);

[ByRefEvent]
public readonly record struct ModularSuitRemovedEvent(EntityUid Suit, EntityUid? User);

[ByRefEvent]
public readonly record struct ModularSuitModuleItemCreatedEvent(EntityUid Module);

[ByRefEvent]
public sealed class ModularSuitModuleAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Suit;

    public ModularSuitModuleAttemptEvent(EntityUid suit)
    {
        Suit = suit;
    }
}

[ByRefEvent]
public readonly record struct ModularSuitModuleToggledEvent(EntityUid Suit, EntityUid? Wearer, bool Activated);
