namespace Content.Shared.Lavaland.Mobs;

[ByRefEvent]
public record struct MegafaunaKilledEvent(EntityUid Megafauna, EntityUid? Killer);

[ByRefEvent]
public record struct MegafaunaStartupEvent();

[ByRefEvent]
public record struct MegafaunaDeinitEvent();

[ByRefEvent]
public record struct MegafaunaAttackEvent(EntityUid Target);

[ByRefEvent]
public record struct MegafaunaRageEvent(bool IsEnraged);

[ByRefEvent]
public record struct MegafaunaPhaseChangedEvent(int OldPhase, int NewPhase);
