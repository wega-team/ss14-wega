using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Strangulation;

[Serializable, NetSerializable]
public sealed partial class StrangulationDelayDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class StrangulationDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class BreakFreeDoAfterEvent : SimpleDoAfterEvent { }

public sealed partial class BreakFreeStrangleAlertEvent : BaseAlertEvent;
