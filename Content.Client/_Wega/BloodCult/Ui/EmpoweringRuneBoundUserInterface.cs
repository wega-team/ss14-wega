using Content.Shared.Blood.Cult;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.BloodCult.Ui;

public sealed class EmpoweringRuneBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private EmpoweringRuneMenu? _menu;

    public EmpoweringRuneBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<EmpoweringRuneMenu>();
        _menu.OnSelectSpell += spell =>
        {
            SendMessage(new EmpoweringRuneSelectSpellMessage(spell));
            Close();
        };

        _menu.OpenCentered();
    }
}
