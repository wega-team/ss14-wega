using Robust.Shared.Audio;

namespace Content.Shared._Wega.Implants.Components;

/// <summary>
/// данные для имплантации контроля над разумом.
/// </summary>
[RegisterComponent]
public sealed partial class MindControlImplantComponent : Component
{
    public EntityUid Master = EntityUid.Invalid;

    [DataField]
    public LocId BriefingText = "mind-control-user-briefing";

    [DataField]
    public LocId DebriefingText = "mind-control-user-freed";

    [DataField]
    public SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");
}
