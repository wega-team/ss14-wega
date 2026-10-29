using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ERP.Components
{
    [RegisterComponent]
    public sealed partial class SexToyComponent : Component
    {
        [DataField]
        public List<string> Prototype = new();
    }

    [RegisterComponent]
    public sealed partial class VibratorComponent : Component
    {
    }

    [RegisterComponent]
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
