using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpiderOSComponent : Component
{
    public static readonly string[] SuitGenders       = { "Male", "Female" };
    public static readonly string[] SuitStyleVariants = { "Classic", "New" };
    public static readonly string[] SuitColors        = { "Red", "Blue", "Green" };

    [DataField]
    public EntProtoId SuitProto = "ClothingOuterSuitSpaceNinja";

    [DataField]
    public EntProtoId MaskProto = "ClothingMaskNinja";

    [DataField]
    public EntProtoId HelmetProto = "ClothingHeadHelmetSpaceNinja";

    [DataField]
    public EntProtoId GlovesProto = "ClothingHandsGlovesSpaceNinja";

    [DataField]
    public EntProtoId ShoesProto = "ClothingShoesSpaceNinja";

    [DataField, AutoNetworkedField]
    public int SuitGender;        // 0 = Male, 1 = Female

    [DataField, AutoNetworkedField]
    public int SuitStyleVariant;  // 0 = Classic, 1 = New

    [DataField, AutoNetworkedField]
    public int SuitColor = 2;     // 0 = Red, 1 = Blue, 2 = Green

    [DataField, AutoNetworkedField]
    public bool IsActivated;

    /// <summary>
    /// One entry per ability row (5 rows). Value = column index chosen (0=Ghost, 1=Snake, 2=Steel).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int[] AbilityChoices = { 0, 0, 0, 0, 0 };

    public static string GetSuitEquippedState(int gender, int styleVariant, int color)
    {
        var style = styleVariant == 0 ? "classic" : "new";
        var col   = color switch { 0 => "red", 1 => "blue", _ => "green" };
        var f     = gender == 1 ? "_f" : "";
        return $"equipped_ninja_suit_{style}_{col}{f}";
    }

    public static string GetMaskEquippedState(int styleVariant, int color)
    {
        var style = styleVariant == 0 ? "classic" : "new";
        var col   = color switch { 0 => "red", 1 => "blue", _ => "green" };
        return $"equipped-ninja_mask_{style}_{col}";
    }

    public static string GetGlovesEquippedState(int styleVariant, int color)
    {
        var style = styleVariant == 0 ? "classic" : "new";
        var col   = color switch { 0 => "red", 1 => "blue", _ => "green" };
        return $"equipped_ninja_gloves_{style}_{col}";
    }

    public static string GetHelmetEquippedState(int suitStyleVariant)
    {
        return suitStyleVariant == 1 ? "equipped-ninja_hood_new" : "equipped-ninja_hood_classic";
    }

    public static string GetKatanaEquippedBeltState(int color)
    {
        return color switch { 0 => "equipped-BELT-red", 1 => "equipped-BELT-blue", _ => "equipped-BELT-green" };
    }

    // Combined style+color index for headset/mask/gloves icon GenericVisualizer (0-5)
    public static int GetHeadsetVariant(int styleVariant, int color) => styleVariant * 3 + color;
    public static int GetMaskVariant(int styleVariant, int color)    => styleVariant * 3 + color;
    public static int GetGlovesVariant(int styleVariant, int color)  => styleVariant * 3 + color;

    // Combined style+gender+color index for suit icon GenericVisualizer (0-11)
    public static int GetSuitVariant(int gender, int styleVariant, int color) => styleVariant * 6 + gender * 3 + color;

    // Helmet icon index (0=classic, 1=new)
    public static int GetHelmetVariant(int styleVariant) => styleVariant;

    // Server-only runtime state — not serialized or networked
    public EntityUid? LinkedShuttle;
    public EntityUid? LinkedFacility;
    public bool IsInitializing;
    public string? CurrentTerminalLine;
    public bool TerminalFinished;
}
