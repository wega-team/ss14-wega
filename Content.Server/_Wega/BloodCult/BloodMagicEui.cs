using Content.Server.EUI;
using Content.Shared.Blood.Cult;
using Content.Shared.Eui;

namespace Content.Server.Blood.Cult.UI;

/// <summary>
/// Logic for the blood magic window
/// </summary>
public sealed class BloodMagicEui(EntityUid cultist, BloodCultSystem bloodCult) : BaseEui
{
    public override EuiStateBase GetNewState()
        => new BloodMagicState();

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is BloodMagicSelectSpellMessage msgSpell)
            bloodCult.AfterSpellSelect(cultist, msgSpell.Spell);

        Close();
    }
}
