using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

// Base
public sealed partial class VampireSelectClassActionEvent : InstantActionEvent { }

public sealed partial class VampireRejuvenateActionEvent : InstantActionEvent { }

public sealed partial class VampireGlareActionEvent : EntityTargetActionEvent { }

public sealed partial class VampireDrinkingBloodActionEvent : EntityTargetActionEvent { }

[Serializable, NetSerializable]
public sealed partial class VampireDrinkingBloodDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public float Volume = 0;
}

[Serializable, NetSerializable]
public sealed class SelectClassPressedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }

    public SelectClassPressedEvent(NetEntity uid)
    {
        Uid = uid;
    }
}

[Serializable, NetSerializable]
public sealed class VampireSelectClassMenuClosedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public string SelectedClass { get; }

    public VampireSelectClassMenuClosedEvent(NetEntity uid, string selectedClass)
    {
        Uid = uid;
        SelectedClass = selectedClass;
    }
}

// Hemomancer Abilities
public sealed partial class VampireClawsActionEvent : InstantActionEvent { }

public sealed partial class VampireBloodTentacleAction : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId EntityId = "EffectBloodTentacleSpawn";

    [DataField]
    public List<Direction> OffsetDirections = new()
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
        Direction.NorthEast,
        Direction.NorthWest,
        Direction.SouthEast,
        Direction.SouthWest,
    };

    [DataField]
    public int ExtraSpawns = 8;
}

public sealed partial class VampireBloodBarrierActionEvent : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId EntityId = "BloodBarrier";

    public bool UseCasterDirection { get; set; } = true;
}

public sealed partial class VampireSanguinePoolActionEvent : InstantActionEvent
{
    [DataField]
    public string PolymorphProto = "VampireBlood";

    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
}

public sealed partial class VampirePredatorSensesActionEvent : InstantActionEvent
{
    [DataField]
    public string Proto = "PuddleBlood";

    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
}

public sealed partial class VampireBloodEruptionActionEvent : InstantActionEvent { }

public sealed partial class VampireBloodBringersRiteActionEvent : InstantActionEvent
{
    [DataField]
    public string Proto = "PuddleBlood";

    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
}

// Umbrae Abilities
public sealed partial class VampireCloakOfDarknessActionEvent : InstantActionEvent { }

public sealed partial class VampireShadowSnareActionEvent : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId EntityId = "ShadowTrap";
}

public sealed partial class VampireSoulAnchorActionEvent : InstantActionEvent { }

[Serializable, NetSerializable]
public sealed partial class SoulAnchorDoAfterEvent : SimpleDoAfterEvent { }

public sealed partial class VampireDarkPassageActionEvent : WorldTargetActionEvent { }

public sealed partial class VampireExtinguishActionEvent : InstantActionEvent { }

public sealed partial class VampireShadowBoxingActionEvent : EntityTargetActionEvent { }

public sealed partial class VampireEternalDarknessActionEvent : InstantActionEvent { }

[Serializable, NetSerializable]
public sealed partial class VampireToggleFovEvent : EntityEventArgs
{
    public NetEntity User { get; }

    public VampireToggleFovEvent(NetEntity user)
    {
        User = user;
    }
}


// Gargantua Abilities
public sealed partial class VampireRejuvenateAdvancedActionEvent : InstantActionEvent { }

public sealed partial class VampireBloodSwellActionEvent : InstantActionEvent { }

public sealed partial class VampireBloodRushActionEvent : InstantActionEvent { }

public sealed partial class VampireSeismicStompActionEvent : InstantActionEvent
{
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Footsteps/largethud.ogg");
}

public sealed partial class VampireBloodSwellAdvancedActionEvent : InstantActionEvent { }

public sealed partial class VampireOverwhelmingForceActionEvent : InstantActionEvent { }

public sealed partial class VampireDemonicGraspActionEvent : EntityTargetActionEvent { }

public sealed partial class VampireChargeActionEvent : WorldTargetActionEvent
{
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Footsteps/largethud.ogg");
}

// Dantalion Abilities
public sealed partial class MaxThrallCountUpdateEvent : InstantActionEvent { }

public sealed partial class VampireEnthrallActionEvent : EntityTargetActionEvent { }

[Serializable, NetSerializable]
public sealed partial class EnthrallDoAfterEvent : SimpleDoAfterEvent
{
    public new NetEntity Target { get; set; }

    public EnthrallDoAfterEvent(NetEntity target)
    {
        Target = target;
    }
}

public sealed partial class VampireCommuneActionEvent : InstantActionEvent { }

public sealed partial class VampirePacifyActionEvent : EntityTargetActionEvent { }

public sealed partial class VampireSubspaceSwapActionEvent : EntityTargetActionEvent { }

//public sealed partial class VampireDeployDecoyActionEvent : InstantActionEvent { }

public sealed partial class VampireRallyThrallsActionEvent : InstantActionEvent { }

public sealed partial class VampireBloodBondActionEvent : InstantActionEvent { }

public sealed partial class VampireMassHysteriaActionEvent : InstantActionEvent { }
