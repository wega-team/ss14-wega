namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(HeraldSystem))]
public sealed partial class HeraldComponent : Component
{
    [ViewVariables]
    public List<EntityUid> Mirrors = new();
}

[RegisterComponent, Access(typeof(HeraldSystem))]
public sealed partial class HeraldMirrorComponent : Component
{
    [ViewVariables] public EntityUid OwnerHerald;
}
