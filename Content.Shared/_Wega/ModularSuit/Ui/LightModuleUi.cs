using Robust.Shared.Serialization;

namespace Content.Shared.Modular.Suit;

[Serializable, NetSerializable]
public enum LightModuleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class LightModuleBoundUserInterfaceState : BoundUserInterfaceState
{
    public Color LightColor { get; }
    public bool Multicoloured { get; }

    public LightModuleBoundUserInterfaceState(Color lightColor, bool multicoloured)
    {
        LightColor = lightColor;
        Multicoloured = multicoloured;
    }
}

[Serializable, NetSerializable]
public sealed class UpdateLightModuleMessage : BoundUserInterfaceMessage
{
    public Color LightColor { get; }
    public bool Multicoloured { get; }

    public UpdateLightModuleMessage(Color lightColor, bool multicoloured)
    {
        LightColor = lightColor;
        Multicoloured = multicoloured;
    }
}
