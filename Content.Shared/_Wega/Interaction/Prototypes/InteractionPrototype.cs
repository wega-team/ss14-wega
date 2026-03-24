using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Interaction;

[Prototype]
public sealed partial class InteractionActionPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<InteractionActionPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    /// The localized name of the action displayed to the player.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public LocId Name { get; private set; } = default!;

    /// <summary>
    /// Icon for verb/interaction menu. Can be null.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public SpriteSpecifier? Icon { get; private set; }

    /// <summary>
    /// Is it allowed to apply an action to itself (user = target).
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public bool CanInteractSelf { get; private set; }

    /// <summary>
    /// Only self-triggering: the action is only available when user = target.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public bool OnlyInteractSelf { get; private set; }

    /// <summary>
    /// Action priority when sorting the list of verbs.
    /// The higher the priority, the earlier it is displayed/executed.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public int Priority { get; private set; } = 0;

    /// <summary>
    /// The recharge time (in seconds) after performing an action.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public float Cooldown { get; private set; }

    /// <summary>
    /// Delay before applying effects (in seconds).
    /// If greater than 0, do-after is triggered with the specified delay.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public float Delay { get; private set; } = 0;

    /// <summary>
    /// Localized message for the initiator when the delay starts.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public LocId? DelayStartUserMessage { get; private set; } = default!;

    /// <summary>
    /// Localized message for the target when the delay starts.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public LocId? DelayStartTargetMessage { get; private set; } = default!;

    /// <summary>
    /// Localized message for others when the delay starts.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public LocId? DelayStartOtherMessage { get; private set; } = default!;

    /// <summary>
    /// The color of the popup message (for example, <see cref="Color.Red"/> for warnings).
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public Color? DelayStartColorMessage { get; private set; } = default!;

    /// <summary>
    /// A list of conditions that must be met for the action to be available.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<InteractionCondition> Conditions { get; private set; } = new();

    /// <summary>
    /// A list of effects that are applied when an action is performed.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<InteractionEffect> Effects { get; private set; } = new();
}
