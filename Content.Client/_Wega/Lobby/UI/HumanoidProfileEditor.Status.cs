using Content.Shared.Humanoid;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void SetStatus(Status newStatus)
    {
        Profile = Profile?.WithStatus(newStatus);
        switch (newStatus)
        {
            case Status.No: Profile = Profile?.WithStatus(Status.No); break;
            case Status.Semi: Profile = Profile?.WithStatus(Status.Semi); break;
            case Status.Full: Profile = Profile?.WithStatus(Status.Full); break;
            case Status.Absolute: Profile = Profile?.WithStatus(Status.Absolute); break;
            default: Profile = Profile?.WithStatus(Status.No); break;
        }

        UpdateStatusControls();
        ReloadPreview();
    }

    private void UpdateStatusControls()
    {
        if (Profile == null)
        {
            return;
        }

        StatusButton.SelectId((int)Profile.Status);
    }
}
