using Content.Shared.Item.Selector.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Item.Selector.UI;

[UsedImplicitly]
public sealed class ObjectSelectorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ObjectSelectorWindow? _window;

    public ObjectSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ObjectSelectorWindow>();

        _window.OnObjectSelected += (selectedId) =>
        {
            SendMessage(new ObjectSelectorSelectionMessage(selectedId));
            _window.Close();
        };
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null || message is not ObjectSelectorUserMessage msg)
            return;

        _window.Populate(msg);
    }
}
