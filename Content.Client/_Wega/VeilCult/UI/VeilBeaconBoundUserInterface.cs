using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Veil.Cult.UI;

public sealed class VeilBeaconBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [ViewVariables]
    private VeilBeaconWindow? _window;

    public VeilBeaconBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VeilBeaconWindow>();

        if (_entManager.TryGetComponent(Owner, out VeilCultBeaconComponent? beacon))
        {
            _window.SetMaxLabelLength(beacon.MaxLabelChars);
        }

        _window.OnNameChanged += OnNameChanged;
        Reload();
        _window.SetInitialNameState(); 
    }

    private void OnNameChanged(string newName)
    {
        if (_entManager.TryGetComponent(Owner, out VeilCultBeaconComponent? beacon) &&
            beacon.AssignedLabel.Equals(newName))
            return;

        SendPredictedMessage(new VeilBeaconNameChangedMessage(newName));
    }

    public void Reload()
    {
        if (_window == null || !_entManager.TryGetComponent(Owner, out VeilCultBeaconComponent? component))
            return;

        _window.SetCurrentLabel(component.AssignedLabel);
    }
}
