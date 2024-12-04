using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Swab;

// Corvax-Wega-Disease-start
[Serializable, NetSerializable]
public sealed partial class DiseaseSwabDoAfterEvent : SimpleDoAfterEvent { }
// Corvax-Wega-Disease-end

[Serializable, NetSerializable]
public sealed partial class BotanySwabDoAfterEvent : SimpleDoAfterEvent
{
}
