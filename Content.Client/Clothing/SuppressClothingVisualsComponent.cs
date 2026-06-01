namespace Content.Client.Clothing;

/// <summary>
/// Suppresses clothing visual re-rendering while an external system (e.g. chameleon disguise)
/// manages the entity's sprite directly via CopySprite.
/// </summary>
[RegisterComponent]
public sealed partial class SuppressClothingVisualsComponent : Component { }
