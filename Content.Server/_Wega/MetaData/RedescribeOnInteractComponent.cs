using Robust.Shared.Utility;

namespace Content.Server.Wega.MetaData.Components;

/// <summary>
/// Component that allows an entity to be renamed through interaction.
/// </summary>
[RegisterComponent]
public sealed partial class RedescribeOnInteractComponent : Component
{
    /// <summary>
    /// Whether to expose the rename action as an interaction verb.
    /// </summary>
    [DataField]
    public bool UseVerbs { get; set; } = true;

    [DataField]
    public LocId RedescribeActionLocString = "renameable-component-rename-action";

    [DataField]
    public LocId NameTitleLocString = "renameable-component-name-field";

    [DataField]
    public LocId NewNameConditions = "renameable-system-new-name-conditions";
	
	// При необходимости вставить текст кастомный текст в новое описание
    [DataField]
    public LocId DescIn = "change-desc-comp";

    [DataField]
    public LocId NameIn = "namazid-comp";

    [DataField]
    public ResPath VerbTexturePath = new("/Textures/Interface/AdminActions/redescribe.png");
	
    [DataField]
    public int MaxLength = 40;
	
    [DataField]
    public bool Used = false;

	// При необходимости вставить текст кастомный текст в новое описание
    [DataField]
    public bool IsDescIn = false;
	
    [DataField]
    public bool Namenaid = true;

    [DataField]
    public EntityUid? User = null;
}