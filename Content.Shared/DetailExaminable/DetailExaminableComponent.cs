using Content.Shared.Preferences;
using Robust.Shared.GameStates;

namespace Content.Shared.DetailExaminable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DetailExaminableComponent : Component
{
    [DataField, AutoNetworkedField] // Corvax-Wega-Graphomancy-Extended-Edit
    public string Content = string.Empty;

    // Corvax-Wega-Graphomancy-Extended-start
    [DataField, AutoNetworkedField]
    public string CharacterContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string OOCContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string TagsContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string LinksContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string GreenContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string YellowContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string RedContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string NSFWContent { get; set; } = string.Empty;

    public void SetProfile(HumanoidCharacterProfile profile)
    {
        Content = profile.FlavorText;
        CharacterContent = profile.CharacterFlavorText;
        OOCContent = profile.OOCFlavorText;
        TagsContent = profile.TagsFlavorText;
        LinksContent = profile.LinksFlavorText;
        GreenContent = profile.GreenFlavorText;
        YellowContent = profile.YellowFlavorText;
        RedContent = profile.RedFlavorText;
        NSFWContent = profile.NSFWFlavorText;
    }
    // Corvax-Wega-Graphomancy-Extended-end
}
