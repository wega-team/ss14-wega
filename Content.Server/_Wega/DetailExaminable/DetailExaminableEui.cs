using Content.Server.EUI;
using Content.Shared.DetailExaminable;
using Content.Shared.Eui;

namespace Content.Server.DetailExaminable;

public sealed class DetailExaminableEui : BaseEui
{
    private readonly DetailExaminableEuiState _state;

    public DetailExaminableEui(DetailExaminableEuiState state)
    {
        _state = state;
    }

    public override EuiStateBase GetNewState()
    {
        return _state;
    }
}
