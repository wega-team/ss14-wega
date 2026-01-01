using Content.Shared.Blood.Cult;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.BloodCult.Ui;

public sealed class SummoningRuneBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SummoningRuneMenu? _menu;

    public SummoningRuneBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SummoningRuneMenu>();
        _menu.OnCultistSelected += cultist =>
        {
            SendMessage(new SummoningRuneSelectCultistMessage(cultist));
            Close();
        };

        _menu.OpenCentered();
    }
}
