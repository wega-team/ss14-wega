using Robust.Shared.Serialization;

namespace Content.Shared.Ninja;

[Serializable, NetSerializable]
public enum NinjaColorVisuals : byte
{
    KatanaColor,
    HeadsetVariant,
    SuitVariant,    // style*6 + gender*3 + color (0-11)
    MaskVariant,    // style*3 + color (0-5)
    HelmetVariant,  // style (0-1)
    GlovesVariant,  // style*3 + color (0-5)
}

[Serializable, NetSerializable]
public enum NinjaColorLayer : byte
{
    KatanaBase,
    HeadsetBase,
    SuitBase,
    MaskBase,
    HelmetBase,
    GlovesBase,
}
