using Content.Shared.Hands.EntitySystems;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction;

/// <summary>
/// The specified entity must have an item in its hand
/// of a specific prototype or tool with the required quality.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class HasItemInHandCondition : InteractionCondition
{
    /// <summary>
    /// The whitelist to check against. <see cref="EntityWhitelist"/>.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist { get; private set; }

    /// <summary>
    /// A prototype of an object that should be held in the hand.
    /// </summary>
    [DataField("item")]
    public EntProtoId? ItemPrototype { get; private set; }

    /// <summary>
    /// The quality of the tool that is being tested.
    /// </summary>
    [DataField("tool")]
    public ProtoId<ToolQualityPrototype>? ToolQuality { get; private set; }

    /// <summary>
    /// Which entity to check: user or target. <see cref="ConditionTarget"/>
    /// </summary>
    [DataField]
    public ConditionTarget Target { get; private set; } = ConditionTarget.User;

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var checkEntity = Target == ConditionTarget.User ? user : target;

        var handsSystem = entityManager.System<SharedHandsSystem>();
        if (!handsSystem.TryGetActiveItem(checkEntity, out var item))
            return false;

        if (Whitelist != null)
        {
            var whitelistSystem = entityManager.System<EntityWhitelistSystem>();
            if (!whitelistSystem.IsWhitelistPass(Whitelist, item.Value))
                return false;
        }

        if (ItemPrototype != null)
        {
            var meta = entityManager.GetComponent<MetaDataComponent>(item.Value);
            if (meta.EntityPrototype?.ID != ItemPrototype)
                return false;
        }

        if (ToolQuality != null)
        {
            var toolSystem = entityManager.System<SharedToolSystem>();
            if (!toolSystem.HasQuality(item.Value, ToolQuality))
                return false;
        }

        return true;
    }
}
