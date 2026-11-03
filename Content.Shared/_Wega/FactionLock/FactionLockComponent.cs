using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.FactionLock;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(FactionLockSystem))]
public sealed partial class FactionLockComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();

    [DataField, AutoNetworkedField]
    public bool BlockShooting = true;

    [DataField, AutoNetworkedField]
    public bool BlockMelee = true;

    [DataField]
    public LocId DenyMessage = "faction-lock-deny";

    [DataField]
    public SoundSpecifier? DenySound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField]
    public LocId? ExamineText;

    [ViewVariables]
    public TimeSpan NextDeny;
}
