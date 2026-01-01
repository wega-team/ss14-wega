using Content.Client.Eui;
using Content.Shared.Blood.Cult;
using JetBrains.Annotations;

namespace Content.Client._Wega.BloodCult.Ui;

[UsedImplicitly]
public sealed class BloodMagicEui : BaseEui
{
    private readonly BloodMagicMenu _menu;

    public BloodMagicEui()
    {
        _menu = new BloodMagicMenu();
        _menu.OnSelectedSpell += spell =>
        {
            SendMessage(new BloodMagicSelectSpellMessage(spell));
            _menu.Close();
        };
    }

    public override void Opened()
    {
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        _menu.Close();
    }
}
