using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Magic;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Modular.Suit;

public sealed partial class ToggleLightModuleEvent : InstantActionEvent
{
    [DataField]
    public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Items/flashlight_on.ogg");

    [DataField]
    public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/Items/flashlight_off.ogg");
}

public sealed partial class ActivateTeleporterModuleEvent : WorldTargetActionEvent
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

public sealed partial class AntiGravitationEvent : InstantActionEvent
{
    [DataField]
    public SoundSpecifier ActivationSound = new SoundCollectionSpecifier("RadiationPulse");
}

[Serializable, NetSerializable]
public sealed partial class ModuleGrabberDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ModuleMicrowaveDoAfterEvent : SimpleDoAfterEvent;

public sealed partial class ModuleHealSurgeyEvent : InstantActionEvent;

public sealed partial class ModuleEMPEvent : InstantActionEvent;

public sealed partial class ModuleStealthEvent : InstantActionEvent
{
    [DataField]
    public float Coefficient = 0.3f;
}

public sealed partial class DamageOnActionModuleEvent : InstantActionEvent
{
    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField]
    public float HungerPerUse = 35f;
}