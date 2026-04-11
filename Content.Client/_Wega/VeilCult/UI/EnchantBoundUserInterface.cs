using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Veil.Cult.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Veil.Cult.UI;

[UsedImplicitly]
public sealed class EnchantBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

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
        if (_playerManager.LocalSession?.AttachedEntity is not { } user)
			return;
        SendMessage(new EnchantSelectedMessage(_entMan.GetNetEntity(user), entId));
        Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is EnchantSelectionState cast)
            _window?.Populate(cast);
    }
}
