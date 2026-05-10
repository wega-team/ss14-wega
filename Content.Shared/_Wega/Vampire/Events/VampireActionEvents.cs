using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Polymorph;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

/// <summary>
/// Interface for vampire action events that require blood costs.
/// Allows you to unify the verification and deduction of blood costs through the <see cref="BloodCost"/>.
/// </summary>
public interface IVampireActionEvent
{
    FixedPoint2 BloodCost { get; }
}

// Base
public sealed partial class VampireSelectClassActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireRejuvenateActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public int Repeats = 5;

    [DataField]
    public DamageSpecifier Heal = default!;

    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(3.5);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireGlareActionEvent : EntityTargetActionEvent { }

public sealed partial class VampireDrinkingBloodActionEvent : EntityTargetActionEvent { }

// Hemomancer Abilities
public sealed partial class VampireClawsActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<StartingGearPrototype> ProtoId = "VampireClawsGear";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBloodTentacleAction : WorldTargetActionEvent, IVampireActionEvent
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

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBloodBarrierActionEvent : WorldTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId EntityId = "BloodBarrier";

    public bool UseCasterDirection { get; set; } = true;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireSanguinePoolActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphProto = "VampireBlood";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampirePredatorSensesActionEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId EntityId = "PuddleBlood";

    [DataField]
    public SoundSpecifier Sound;
}

public sealed partial class VampireBloodEruptionActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBloodBringersRiteActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId EntityId = "PuddleBlood";

    [DataField]
    public ProtoId<AlertPrototype> Alert = "AlertBloodRite";

    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField]
    public DamageSpecifier Heal = default!;

    [DataField]
    public float StaminaMod = -15f;

    [DataField]
    public SoundSpecifier Sound;

    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(1);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

// Umbrae Abilities
public sealed partial class VampireCloakOfDarknessActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public float SpeedMod = 1.3f;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireShadowSnareActionEvent : WorldTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId EntityId = "ShadowTrap";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireSoulAnchorActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireDarkPassageActionEvent : WorldTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId MistEffect = "VampireMistEffect";

    [DataField]
    public EntProtoId MistReappearEffect = "VampireMistReappearEffect";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireExtinguishActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireShadowBoxingActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId EntityId = "MobFollowerShadow";

    [DataField]
    public int Repeats = 10;

    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(1);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireEternalDarknessActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<AlertPrototype> Alert = "AlertEternalDarkness";

    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(1);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

[Serializable, NetSerializable]
public sealed partial class VampireToggleFovEvent : EntityEventArgs
{
    public NetEntity User { get; }
    public bool Enabled { get; }

    public VampireToggleFovEvent(NetEntity user, bool enabled)
    {
        User = user;
        Enabled = enabled;
    }
}


// Gargantua Abilities
public sealed partial class VampireBloodSwellActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(30);

    [DataField]
    public bool Advanced = false;

    [DataField]
    public string BonusDamageType = "Blunt";

    [DataField]
    public float BonusDamageAmount = 14f;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBloodRushActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(10);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireSeismicStompActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public SoundSpecifier Sound;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireOverwhelmingForceActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireDemonicGraspActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireChargeActionEvent : WorldTargetActionEvent, IVampireActionEvent
{
    [DataField("components")]
    public ComponentRegistry EnsurableComponents;

    [DataField]
    public SoundSpecifier Sound;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

// Dantalion Abilities
public sealed partial class MaxThrallCountUpdateEvent : InstantActionEvent { }

public sealed partial class VampireEnthrallActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireCommuneActionEvent : InstantActionEvent { }

public sealed partial class VampirePacifyActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(40);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireSubspaceSwapActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireDeployDecoyActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireRallyThrallsActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBloodBondActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(1);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireMassHysteriaActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireThrallHealActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public int Repeats = 3;

    [DataField]
    public DamageSpecifier Heal = default!;

    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(4);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampirePacifyNearbyActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(8);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}
