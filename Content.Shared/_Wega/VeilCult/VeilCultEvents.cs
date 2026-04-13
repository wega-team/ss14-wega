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


public sealed partial class CrusherEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class ConfusionEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class KnockbackEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class SwordsmenEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class BloodshedEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class HasteEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class ReflectionEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class CamouflageEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class AbsorbEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class FlashEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class HardenPlatesEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class NorthStarEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class RedFlameEnchantActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class VeilCultTeleportDoAfterEvent : SimpleDoAfterEvent
{
}

