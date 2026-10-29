using Content.Server.NPC.Systems;

namespace Content.Server.NPC.Components;

[RegisterComponent, Access(typeof(NPCOptimizationSystem))]
public sealed partial class NPCIgnoringOptimizeComponent : Component;
