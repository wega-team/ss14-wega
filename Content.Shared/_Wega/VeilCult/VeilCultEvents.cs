using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Veil.Cult;

// Events
public sealed partial class VeilCultMidasTouchActionEvent : InstantActionEvent
{
}

public sealed partial class VeilCultMidasTouchGetHandEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class EnchantingDoAfterEvent : SimpleDoAfterEvent
{
	public EntProtoId Entity;
}

