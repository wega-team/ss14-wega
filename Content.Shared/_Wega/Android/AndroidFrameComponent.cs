using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Android;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AndroidFrameComponent : Component
{
    [DataField]
    public ProtoId<SpeciesPrototype> Species = "Android";

    [DataField]
    public string BatterySlot = "battery";
    [DataField]
    public string BrainSlot = "brain";

    [DataField]
    public SoundSpecifier AssembleSound = new SoundPathSpecifier("/Audio/Items/rped.ogg");

    [AutoNetworkedField]
    public HumanoidCharacterProfile? Profile;
}

[Serializable, NetSerializable]
public enum AndroidConstructUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class AndroidConstructUiState : BoundUserInterfaceState
{
    public AndroidConstructUiState(ProtoId<SpeciesPrototype> species, HumanoidCharacterProfile profile, bool hasBattery, bool hasBrain)
    {
        Species = species;
        Profile = profile;

        HasBattery = hasBattery;
        HasBrain = hasBrain;
    }

    public readonly ProtoId<SpeciesPrototype> Species;
    public readonly HumanoidCharacterProfile Profile;

    public readonly bool HasBattery, HasBrain;
}

[Serializable, NetSerializable]
public sealed partial class AndroidConstructEditMessage : BoundUserInterfaceMessage
{
    public AndroidConstructEditMessage(HumanoidCharacterProfile newProfile)
    {
        NewProfile = newProfile;
    }

    public readonly HumanoidCharacterProfile NewProfile;
}

[Serializable, NetSerializable]
public sealed partial class AndroidConstructAssembleMessage : BoundUserInterfaceMessage
{
    public AndroidConstructAssembleMessage()
    {

    }
}

public enum AndroidFrameVisualLayers : byte
{
    Frame,
}

[Serializable, NetSerializable]
public enum AndroidFrameVisuals
{
    ConstructionStage,
}
