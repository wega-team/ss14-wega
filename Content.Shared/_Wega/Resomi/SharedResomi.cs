using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Actions.Components;

namespace Content.Shared.Resomi;

public sealed partial class SwitchAgillityActionEvent : InstantActionEvent;

public sealed partial class ListenUpActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class ListenUpDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Rises when the action state changes
/// </summary>
/// <param name="Action"> Entity of Action that we want change the state</param>
/// <param name="Toggled"> </param>
[ByRefEvent]
public readonly record struct SwitchAgillity(Entity<ActionComponent> Action, bool Toggled);
