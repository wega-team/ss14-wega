using Robust.Shared.GameObjects;

namespace Content.Shared._Wega.Stunnable.Components;

[RegisterComponent]
public sealed partial class StunOtherOnHitComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(3);
}