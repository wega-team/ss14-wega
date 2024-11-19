using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ERP.Components
{
    [RegisterComponent]
    [ComponentProtoName("SexToy")]
    public sealed partial class SexToyComponent : Component
    {
        [DataField]
        public List<string> Prototype = new();
    }

    [RegisterComponent]
    [ComponentProtoName("Vibrator")]
    public sealed partial class VibratorComponent : Component
    {
    }

    [RegisterComponent]
    [ComponentProtoName("Strapon")]
    public sealed partial class StraponComponent : Component
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class InteractionDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class SexToyDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class VibratorDoAfterEvent : SimpleDoAfterEvent
{
}
