using Content.Shared.Tag;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery;

[Prototype("surgeryGraph")]
public sealed class SurgeryGraphPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("startNodes", required: true)]
    public List<ProtoId<SurgeryNodePrototype>> StartNodeIds { get; } = new();

    public IEnumerable<SurgeryNodePrototype> GetStartNodes()
    {
        var protoManager = IoCManager.Resolve<IPrototypeManager>();
        foreach (var startNodeId in StartNodeIds)
        {
            if (protoManager.TryIndex(startNodeId, out var node))
            {
                yield return node;
            }
        }
    }
}

[Prototype("surgeryNode")]
[Serializable, DataDefinition]
public sealed partial class SurgeryNodePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("description")]
    public string Description { get; private set; } = string.Empty;

    [DataField("bodyPart")]
    public string? BodyPart { get; private set; }

    [DataField("packages")]
    public List<ProtoId<SurgeryPackagePrototype>> PackageIds { get; private set; } = new();

    [DataField("transitions")]
    public List<ProtoId<SurgeryTransitionPrototype>> TransitionIds { get; private set; } = new();

    // Returns all transitions from this node and its packages.
    public IEnumerable<ProtoId<SurgeryTransitionPrototype>> AllTransitions
    {
        get
        {
            var all = new List<ProtoId<SurgeryTransitionPrototype>>();
            var protoManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var packageId in PackageIds)
            {
                if (protoManager.TryIndex(packageId, out SurgeryPackagePrototype? package))
                {
                    all.AddRange(package.TransitionIds);
                }
            }

            all.AddRange(TransitionIds);
            return all;
        }
    }
}

[Prototype("surgeryPackage")]
[Serializable, DataDefinition]
public sealed partial class SurgeryPackagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("transitions", required: true)]
    public List<ProtoId<SurgeryTransitionPrototype>> TransitionIds { get; set; } = new();
}

[Prototype("surgeryTransition")]
[Serializable, DataDefinition]
public sealed partial class SurgeryTransitionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("target", required: true)]
    public ProtoId<SurgeryNodePrototype> Target { get; private set; } = default!;

    [DataField("label")]
    public string Label { get; private set; } = string.Empty;

    [DataField("stepGroups")]
    public List<SurgeryStepGroup> StepGroups { get; private set; } = new();
}

[Serializable]
[DataDefinition]
public sealed partial class SurgeryNode
{
    [DataField("name", required: true)]
    public ProtoId<SurgeryNodePrototype> Name { get; set; } = default!;

    [DataField("description")]
    public string Description { get; set; } = string.Empty;

    [DataField("bodyPart")]
    public string? BodyPart { get; set; }

    [DataField("transitions")]
    public List<SurgeryTransition> Transitions { get; set; } = new();
}

[Serializable]
[DataDefinition]
public sealed partial class SurgeryTransition
{
    [DataField("target", required: true)]
    public ProtoId<SurgeryNodePrototype> Target { get; set; } = default!;

    [DataField("label")]
    public string Label { get; set; } = string.Empty;

    [DataField("stepGroups")]
    public List<SurgeryStepGroup> StepGroups { get; set; } = new();
}

[Serializable]
[DataDefinition]
public sealed partial class SurgeryStepGroup
{
    [DataField("parallel")]
    public bool Parallel { get; private set; } = false;

    [DataField("steps")]
    public List<SurgeryStep> Steps { get; private set; } = new();

    [DataField("conditions")]
    public List<SurgeryStepCondition> Conditions { get; private set; } = new();
}

[Serializable]
[DataDefinition]
public sealed partial class SurgeryStep
{
    [DataField("tool")]
    public List<ProtoId<ToolQualityPrototype>>? Tool { get; private set; } = new();

    [DataField("tag")]
    public List<ProtoId<TagPrototype>>? Tag { get; private set; } = new();

    [DataField("action", required: true)]
    public SurgeryActionType Action { get; private set; }

    [DataField("conditions")]
    public List<SurgeryStepCondition> Conditions { get; private set; } = new();

    [DataField("sound")]
    public SoundSpecifier? Sound { get; set; } = default!;

    [DataField("requiredPart")]
    public string? RequiredPart { get; private set; }

    [DataField("requiredImplant")]
    public string? RequiredImplant { get; private set; }

    [DataField("entityPreview")]
    public string? EntityPreview { get; private set; }

    [DataField("damageType")]
    public ProtoId<InternalDamagePrototype>? DamageType { get; private set; }

    [DataField("time")]
    public float Time { get; private set; } = 1f;

    [DataField("successChance")]
    public float SuccessChance { get; private set; } = 1f;

    [DataField("failureEffect")]
    public List<SurgeryFailedType>? FailureEffect { get; private set; }
}

[Serializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class SurgeryStepCondition
{
    [DataField("invert")]
    public bool Invert { get; private set; } = false;

    public bool CheckWithInvert(EntityUid patient, IEntityManager entityManager)
    {
        var result = Check(patient, entityManager);
        return Invert ? !result : result;
    }

    public abstract bool Check(EntityUid patient, IEntityManager entityManager);
}
