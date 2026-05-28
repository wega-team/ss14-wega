using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class SlowdownOnContactComponent : Component
{
    [DataField]
    public string FixtureId = "fix";

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    [DataField]
    public float Multiplier = 1f;

    [DataField]
    public EntityWhitelist Blacklist = new();
}
