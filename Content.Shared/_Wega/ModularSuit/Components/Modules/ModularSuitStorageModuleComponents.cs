namespace Content.Shared.Modular.Suit;

[RegisterComponent]
public sealed partial class ModularSuitStorageModuleComponent : Component
{
    [DataField]
    public string ContainerId = "storagebase";
}
