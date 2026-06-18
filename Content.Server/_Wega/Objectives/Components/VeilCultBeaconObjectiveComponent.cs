using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Task to set up some beacons.
/// </summary>
[RegisterComponent, Access(typeof(VeilCultBeaconObjectiveSystem))]
public sealed partial class VeilCultBeaconObjectiveComponent : Component;
