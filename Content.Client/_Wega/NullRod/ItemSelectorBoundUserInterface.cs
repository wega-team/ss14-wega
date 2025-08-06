using Content.Shared.Item.Selector.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Client._Wega.Item.Selector.UI;

[UsedImplicitly]
public sealed class ItemSelectorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    [ViewVariables]
    private ItemSelectorWindow? _window;

    public ItemSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ItemSelectorWindow>();

        _window.OnItemSelected += (selectedId) =>
        {
            var netEntity = EntMan.GetNetEntity(_playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid);
            SendMessage(new ItemSelectorSelectionMessage(netEntity, selectedId));
            _window.Close();
        };
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null || message is not ItemSelectorUserMessage msg)
            return;

        _window.Populate(msg);
    }
}
