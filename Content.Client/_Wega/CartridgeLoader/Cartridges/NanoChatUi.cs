using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.CartridgeLoader.Cartridges;

public sealed partial class NanoChatUi : UIFragment
{
    private NanoChatUiFragment? _fragment;
    private NanoChatAddContactPopup? _addContactPopup;

    public override Control GetUIFragmentRoot() => _fragment!;

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new NanoChatUiFragment();
        _addContactPopup = new NanoChatAddContactPopup();

        _fragment.InitializeEmojiPicker();

        _fragment.OpenAddContact += () => _addContactPopup.OpenCentered();
        _fragment.EraseChat += contactId =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(
                new NanoChatUiMessageEvent(new NanoChatEraseContact(contactId))));
        };

        _fragment.OpenEmojiPicker += () => _fragment.OpenEmojiPickerInternal();

        _fragment.OnMutePressed += () =>
            userInterface.SendMessage(new CartridgeUiMessage(
                new NanoChatUiMessageEvent(new NanoChatMuted())));

        _fragment.SetActiveChat += contactId =>
            userInterface.SendMessage(new CartridgeUiMessage(
                new NanoChatUiMessageEvent(new NanoChatSetActiveChat(contactId))));

        _fragment.SendMessage += message =>
        {
            if (_fragment.ActiveChatId != null)
            {
                userInterface.SendMessage(new CartridgeUiMessage(
                    new NanoChatUiMessageEvent(new NanoChatSendMessage(
                        _fragment.ActiveChatId, message))));
            }
        };

        _addContactPopup.OnContactAdded += (contactId, contactName) =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(
                new NanoChatUiMessageEvent(new NanoChatAddContact(contactId, contactName))));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is NanoChatUiState chatState)
            _fragment?.UpdateState(chatState);
    }
}
