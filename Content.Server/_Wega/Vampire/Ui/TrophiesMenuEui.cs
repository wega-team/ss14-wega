using Content.Server.EUI;
using Content.Shared.Vampire;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Server.Vampire;

[UsedImplicitly]
public sealed class TrophiesMenuEui : BaseEui
{
    private readonly TrophiesEuiState _state;

    public TrophiesMenuEui(TrophiesEuiState state)
    {
        _state = state;
    }

    public override EuiStateBase GetNewState() => _state;
}
