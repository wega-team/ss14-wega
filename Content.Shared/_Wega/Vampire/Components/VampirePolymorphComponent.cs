namespace Content.Shared.Vampire.Components;

[RegisterComponent, Access(typeof(SharedVampireSystem))]
public sealed partial class VampirePolymorphComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Body;
}
