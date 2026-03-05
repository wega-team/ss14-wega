using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Borer.WormEvent;

public sealed partial class InfectionBorerEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ImplantEvent : SimpleDoAfterEvent
{
}