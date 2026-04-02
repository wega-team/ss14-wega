using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Modular.Suit;

public sealed partial class ToggleLightModuleEvent : InstantActionEvent
{
    [DataField]
    public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Items/flashlight_on.ogg");

    [DataField]
    public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/Items/flashlight_off.ogg");
}

public sealed partial class ActivateTeleporterModuleEvent : InstantActionEvent
{
    [DataField]
    public SoundSpecifier ActivationSound = new SoundCollectionSpecifier("RadiationPulse");
}

public sealed partial class ToggleHolsterModuleEvent : InstantActionEvent
{
    [DataField]
    public string TargetContainerId = "module_weapon";

    [DataField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

    [DataField]
    public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");
}

public sealed partial class ActivateEnergyShieldModuleEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId ShieldProto = "EnergyShieldEffect";
}

public sealed partial class ActivateDispenserModuleEvent : InstantActionEvent
{
    [DataField]
    public List<EntProtoId> SpawnedProto;

    [DataField]
    public SoundSpecifier ActivateSound = new SoundCollectionSpecifier("RadiationPulse");
}

public sealed partial class ActivateAtrocinatorModuleEvent : InstantActionEvent
{
    [DataField]
    public SoundSpecifier ActivationSound = new SoundCollectionSpecifier("RadiationPulse");
}

public sealed partial class ActivateTanningModuleEvent : InstantActionEvent { }

[Serializable, NetSerializable]
public sealed partial class ModuleGrabberDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ModuleMicrowaveDoAfterEvent : SimpleDoAfterEvent;
