using Content.Shared.Martial.Arts.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Martial.Arts.Components;

/// <summary>
/// A component responsible for assigning a martial art style when wearing clothes.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMartialArtsSystem))]
public sealed partial class MartialArtsClothingComponent : Component
{
    [DataField(required: true)]

    public ProtoId<MartialArtsPrototype> Style;

    [DataField]
    public bool GotMessage = false;

    [DataField]
    public string? EquippedMessage = string.Empty;

    [DataField]
    public string? UnequippedMessage = string.Empty;
}