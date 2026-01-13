using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.Lavaland.Events;

/// <summary>
/// An event triggered when megafauna is killed. For Reward and Specific logic
/// </summary>
/// <param name="Megafauna">ID of the megafauna entity</param>
/// <param name="Killer">The killer's ID, may be null</param>
[ByRefEvent]
public record struct MegafaunaKilledEvent(EntityUid Megafauna, EntityUid? Killer);

[ByRefEvent]
public record struct MegafaunaStartupEvent();

[ByRefEvent]
public record struct MegafaunaDeinitEvent();

/// <summary>
/// An event triggered when a megafauna attacks a target.
/// </summary>
/// <param name="Target">ID of the attack target</param>
[ByRefEvent]
public record struct MegafaunaAttackEvent(EntityUid Target);

public sealed partial class BloodDrunkMinerDashAction : WorldTargetActionEvent
{
    public SoundSpecifier DashSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");
}

public sealed partial class MegaLegionAction : EntityTargetActionEvent
{
}

public sealed partial class ColossusFractionActionEvent : WorldTargetActionEvent
{
}

public sealed partial class ColossusCrossActionEvent : WorldTargetActionEvent
{
}

public sealed partial class ColossusSpriralActionEvent : WorldTargetActionEvent
{
}

public sealed partial class ColossusTripleFractionActionEvent : WorldTargetActionEvent
{
}
