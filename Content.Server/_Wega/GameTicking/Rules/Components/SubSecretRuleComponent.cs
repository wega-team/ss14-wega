using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Random;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(SubSecretRuleSystem))]
public sealed partial class SubSecretRuleComponent : Component
{
    /// <summary>
    /// The gamerules that get added by secret.
    /// </summary>
    [DataField("additionalGameRules")]
    public HashSet<EntityUid> AdditionalGameRules = new();
	
    /// <summary>
    /// Weighted random
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<WeightedRandomPrototype> Secret = "WegaSecretDefault";
}