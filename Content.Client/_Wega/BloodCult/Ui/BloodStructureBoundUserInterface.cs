using Content.Shared.Blood.Cult;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.BloodCult.Ui;

[UsedImplicitly]
public sealed class BloodStructureBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BloodStructureMenu? _menu;

    public BloodStructureBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<BloodStructureMenu>();
        _menu.OnSelectItem += item =>
        {
            SendMessage(new BloodStructureSelectMessage(item));
            Close();
        };

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is BloodStructureBoundUserInterfaceState structureState)
            _menu?.InitializeButtons(structureState);
    }
}
