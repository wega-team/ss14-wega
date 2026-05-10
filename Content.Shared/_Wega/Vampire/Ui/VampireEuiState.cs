using Content.Shared.Eui;
using Content.Shared.Vampire.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

[Serializable, NetSerializable]
public sealed class VampireClassSelectionState : EuiStateBase
{
}

[Serializable, NetSerializable]
public sealed class VampireClassSelectedMessage : EuiMessageBase
{
    public VampireClassEnum SelectedClass { get; }
    public VampireClassSelectedMessage(VampireClassEnum selectedClass) => SelectedClass = selectedClass;
}
