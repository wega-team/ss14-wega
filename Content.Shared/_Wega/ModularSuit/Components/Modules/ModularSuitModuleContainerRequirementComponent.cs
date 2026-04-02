namespace Content.Shared.Modular.Suit;

[RegisterComponent]
public sealed partial class ModularSuitModuleContainerRequirementComponent : Component
{
    [DataField(required: true)]
    public string RequiredContainerId = string.Empty;

    [DataField]
    public LocId FailureMessage = "modsuit-module-requires-item";
}
