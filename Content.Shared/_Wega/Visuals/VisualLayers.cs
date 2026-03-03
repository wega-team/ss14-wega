using Robust.Shared.Serialization;

namespace Content.Shared.Visuals;

[Serializable, NetSerializable]
public enum VisualLayers : byte
{
    Enabled,
    Layer,
    Color
}
