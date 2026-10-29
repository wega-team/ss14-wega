using Robust.Shared.Audio;

namespace Content.Shared.CartridgeLoader.Cartridges;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class NanoChatCartridgeComponent : Component
{
    [DataField]
    public string ChatId = string.Empty;

    [DataField]
    public string? ActiveChat = null;

    [DataField]
    public string OwnerName = "";

    [DataField]
    public bool MutedSound = false;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/pop_high.ogg");

    [DataField]
    public Dictionary<string, ChatContact> Contacts = new();

    [DataField]
    public Dictionary<string, List<ChatMessage>> Messages = new();

    [DataField]
    public Dictionary<string, ChatGroup> Groups = new();

    [DataField, AutoPausedField]
    public TimeSpan NextMessageAllowedAfter = TimeSpan.Zero;

    [DataField]
    public TimeSpan MessageDelay = TimeSpan.FromSeconds(2.5);
}
