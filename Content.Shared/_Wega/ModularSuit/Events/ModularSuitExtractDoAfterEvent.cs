using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Modular.Suit;

[Serializable, NetSerializable]
public sealed partial class ModularSuitExtractDoAfterEvent : SimpleDoAfterEvent
{
    public ModularSuitPart Type { get; }

    public ModularSuitExtractDoAfterEvent(ModularSuitPart type)
    {
        Type = type;
    }
}
