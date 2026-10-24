using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Seraphim;

public sealed partial class SeraphimKneeActionEvent : InstantActionEvent;

public sealed partial class SeraphimFuckOffActionEvent : InstantActionEvent;

public sealed partial class SeraphimConcentrateActionEvent : InstantActionEvent;

public sealed partial class SeraphimOverdriveActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class SeraphimImplantDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
