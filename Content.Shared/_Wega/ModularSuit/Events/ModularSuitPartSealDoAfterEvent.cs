using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Modular.Suit;

[Serializable, NetSerializable]
public sealed partial class ModularSuitPartSealDoAfterEvent : SimpleDoAfterEvent
{
    public bool Activate { get; }

    public ModularSuitPartSealDoAfterEvent(bool activate)
    {
        Activate = activate;
    }
}
