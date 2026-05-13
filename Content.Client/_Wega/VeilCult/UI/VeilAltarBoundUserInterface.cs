using Content.Shared.Veil.Cult.UI;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.Client.Veil.Cult.Ui;

public sealed class VeilAltarBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    
    [ViewVariables]
    private VeilAltarMenu? _menu;

    public VeilAltarBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<VeilAltarMenu>();
        _menu.OnSelectEnergy += user =>
        {
            SendMessage(new VeilAltarSelectEnergyMessage(_entMan.GetNetEntity(user)));
            Close();
        };
        
        _menu.OnSelectOffer += altar =>
        {
            SendMessage(new VeilAltarSelectOfferMessage(_entMan.GetNetEntity(altar)));
            Close();
        };

        _menu.OpenCentered();
    }
    
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is VeilAltarState cast)
            _menu?.UpdateState(cast);
    }
}
