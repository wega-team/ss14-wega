using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class HandCapsuleComponent : Component
{
    [DataField(required: true, customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath CapsulePath { get; private set; }
}
