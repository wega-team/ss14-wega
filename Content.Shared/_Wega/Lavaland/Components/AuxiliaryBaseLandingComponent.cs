using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class AuxiliaryBaseLandingComponent : Component
{
    [DataField(required: true, customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath BasePath { get; private set; }
}
