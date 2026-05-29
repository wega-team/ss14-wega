using Content.Shared._Wega.Android;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._Wega.Android.Ui;

[UsedImplicitly]
public sealed class AndroidConstructBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private AndroidConstructMenu? _menu;

    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public HumanoidCharacterProfile Profile = new();

    public AndroidConstructBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AndroidConstructMenu>();
        _menu.OpenCentered();

        _menu.ProfileChangedAction += OnProfileChanged;
        _menu.AssembleAttemptAction += OnAssembleAttempt;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null || state is not AndroidConstructUiState cast)
            return;

        Profile = cast.Profile;

        _menu.LoadProfile(Profile, _cfgManager, _markingManager, _prototypeManager);
        _menu.UpdateSlotsStatus(cast.HasBattery, cast.HasBrain);
    }

    private void OnAssembleAttempt()
    {
        SendMessage(new AndroidConstructAssembleMessage());
    }

    private void OnProfileChanged(HumanoidCharacterProfile profile, bool needReload)
    {
        if (_menu == null)
            return;

        Profile = profile;
        if (needReload)
            _menu.LoadProfile(Profile, _cfgManager, _markingManager, _prototypeManager);
        else
            _menu.UpdatePreview();

        SendMessage(new AndroidConstructEditMessage(Profile));
    }
}
