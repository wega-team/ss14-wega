using Content.Shared.Veil.Cult.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Wega.VeilCult.UI;

[UsedImplicitly]
public sealed partial class EnchantBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private EnchantWindow? _window;

    public EnchantBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<EnchantWindow>();
        _window.OnEnchantSelected += entId => OnEnchantSelected(entId);
        _window.OnClose += Close;

        _window.OpenCentered();
    }

    private void OnEnchantSelected(EntProtoId entId)
    {
        SendMessage(new EnchantSelectedMessage(entId));
        Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is EnchantSelectionState cast)
            _window?.Populate(cast);
    }
}
