using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="VampireRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(VampireRuleSystem))]
public sealed partial class VampireRuleComponent : Component
{
}
