// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.Disease;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Modifications.Disease.UI;

[UsedImplicitly]
public sealed class DiseaseAnalyzerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DiseaseAnalyzerWindow? _window;

    public DiseaseAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DiseaseAnalyzerWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null)
            return;

        if (message is not DiseaseAnalyzerScannedMessage cast)
            return;

        _window.Populate(cast);
    }
}
