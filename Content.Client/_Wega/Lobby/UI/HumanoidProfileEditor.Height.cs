namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void OnHeightChanged()
    {
        if (Profile == null)
            return;

        var species = _prototypeManager.Index(Profile.Species);
        var newHeight = species.MinHeight + HeightSlider.Value * (species.MaxHeight - species.MinHeight);
        newHeight = (float)Math.Round(newHeight, 2);

        Profile = Profile.WithHeight(newHeight);
        UpdateHeightDisplay();
        ReloadPreview();
    }

    private void UpdateHeightDisplay()
    {
        if (Profile == null)
            return;

        HeightDisplay.Text = Loc.GetString("humanoid-profile-editor-height-display",
            ("value", Profile.Height.ToString("F2")));
    }

    private void UpdateHeightControls()
    {
        if (Profile == null)
            return;

        var species = _prototypeManager.Index(Profile.Species);
        var normalized = (Profile.Height - species.MinHeight) / (species.MaxHeight - species.MinHeight);
        HeightSlider.Value = normalized;

        UpdateHeightDisplay();
    }
}
