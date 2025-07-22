using Content.Shared.Martial.Arts.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Martial.Arts.Components;

/// <summary>
/// The component responsible for assigning actions and processing the logic of martial arts.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMartialArtsSystem))]
public sealed partial class MartialArtsComponent : Component
{
    [DataField]
    public List<ProtoId<MartialArtsPrototype>> Style;

    [DataField]
    public Dictionary<string, EntityUid> AddedActions { get; private set; } = new();
}
