using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

[Serializable, NetSerializable]
public sealed partial class VampireClassSelectionState : EuiStateBase
{
}

[Serializable, NetSerializable]
public sealed partial class VampireClassSelectedMessage : EuiMessageBase
{
    public VampireClassEnum SelectedClass { get; }
    public VampireClassSelectedMessage(VampireClassEnum selectedClass) => SelectedClass = selectedClass;
}
