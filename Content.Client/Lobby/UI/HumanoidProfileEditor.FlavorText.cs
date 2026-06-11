using Content.Client.Stylesheets;
using Content.Shared.Humanoid;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private bool _allowFlavorText;

    private FlavorText.FlavorText? _flavorText;
    private TextEdit? _flavorTextEdit;
    // Corvax-Wega-Graphomancy-Extended-start
    private TextEdit? _flavorTextOOCEdit;
    private TextEdit? _characterTextEdit;
    private TextEdit? _greenTextEdit;
    private TextEdit? _yellowTextEdit;
    private TextEdit? _redTextEdit;
    private TextEdit? _tagsTextEdit;
    private TextEdit? _linksTextEdit;
    private TextEdit? _nsfwTextEdit;
    // Corvax-Wega-Graphomancy-Extended-end

    /// <summary>
    /// Refreshes the flavor text editor status.
    /// </summary>
    public void RefreshFlavorText()
    {
        if (_allowFlavorText)
        {
            if (_flavorText != null)
                return;

            _flavorText = new FlavorText.FlavorText();
            TabContainer.AddChild(_flavorText);
            TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-flavortext-tab"));

            _flavorTextEdit = _flavorText.CFlavorTextInput;
            _flavorTextOOCEdit = _flavorText.CFlavorOOCTextInput;
            _characterTextEdit = _flavorText.CCharacterTextInput;
            _greenTextEdit = _flavorText.CGreenTextInput;
            _yellowTextEdit = _flavorText.CYellowTextInput;
            _redTextEdit = _flavorText.CRedTextInput;
            _tagsTextEdit = _flavorText.CTagsTextInput;
            _linksTextEdit = _flavorText.CLinksTextInput;
            _nsfwTextEdit = _flavorText.CNSFWTextInput;

            UpdateFlavorPreview();

            _flavorText.OnFlavorTextChanged += OnFlavorTextChange;
            _flavorText.OnFlavorOOCTextChanged += OnFlavorOOCTextChange;
            _flavorText.OnCharacterTextChanged += OnCharacterFlavorTextChange;
            _flavorText.OnGreenTextChanged += OnGreenFlavorTextChange;
            _flavorText.OnYellowTextChanged += OnYellowFlavorTextChange;
            _flavorText.OnRedTextChanged += OnRedFlavorTextChange;
            _flavorText.OnTagsTextChanged += OnTagsFlavorTextChange;
            _flavorText.OnLinksTextChanged += OnLinksFlavorTextChange;
            _flavorText.OnNSFWTextChanged += OnNSFWFlavorTextChange;
        }
        else
        {
            if (_flavorText == null)
                return;

            _flavorText.OnFlavorTextChanged -= OnFlavorTextChange;
            _flavorText.OnFlavorOOCTextChanged -= OnFlavorOOCTextChange;
            _flavorText.OnCharacterTextChanged -= OnCharacterFlavorTextChange;
            _flavorText.OnGreenTextChanged -= OnGreenFlavorTextChange;
            _flavorText.OnYellowTextChanged -= OnYellowFlavorTextChange;
            _flavorText.OnRedTextChanged -= OnRedFlavorTextChange;
            _flavorText.OnTagsTextChanged -= OnTagsFlavorTextChange;
            _flavorText.OnLinksTextChanged -= OnLinksFlavorTextChange;
            _flavorText.OnNSFWTextChanged -= OnNSFWFlavorTextChange;

            TabContainer.RemoveChild(_flavorText);

            _flavorTextEdit = null;
            _flavorTextOOCEdit = null;
            _characterTextEdit = null;
            _greenTextEdit = null;
            _yellowTextEdit = null;
            _redTextEdit = null;
            _tagsTextEdit = null;
            _linksTextEdit = null;
            _nsfwTextEdit = null;

            _flavorText = null;
        }
    }

    private void UpdateFlavorPreview()
    {
        if (_flavorText == null || Profile == null)
            return;

        _flavorText.PreviewAppearanceText.SetMessage(Profile.FlavorText);
        _flavorText.PreviewTraitsText.SetMessage(Profile.CharacterFlavorText);
        _flavorText.PreviewOOCText.SetMessage(Profile.OOCFlavorText);
        _flavorText.PreviewTagsText.Text = Profile.TagsFlavorText;

        ProcessLinks(Profile.LinksFlavorText);

        _flavorText.PreviewGYRContainer.RemoveAllChildren();
        CreateGYRBigTextLabel(Loc.GetString($"humanoid-profile-editor-gyr-green"), Color.Green);
        CreateGYRTextLabel(Profile.GreenFlavorText);
        CreateGYRBigTextLabel(Loc.GetString($"humanoid-profile-editor-gyr-yellow"), Color.Yellow);
        CreateGYRTextLabel(Profile.YellowFlavorText);
        CreateGYRBigTextLabel(Loc.GetString($"humanoid-profile-editor-gyr-red"), Color.Red);
        CreateGYRTextLabel(Profile.RedFlavorText);

        _flavorText.PreviewNSFWText.SetMessage(Profile.NSFWFlavorText);

        var species = Loc.GetString($"species-name-{Profile.Species.ToString().ToLower()}");
        var sex = Loc.GetString($"humanoid-profile-editor-sex-{Profile.Sex.ToString().ToLower()}-text");
        var gender = Loc.GetString($"humanoid-profile-editor-pronouns-{Profile.Gender.ToString().ToLower()}-text");

        _flavorText.PreviewNameText.Text = Profile.Name;
        _flavorText.PreviewGenderText.Text = $"{species}|{sex}|{gender}";
        _flavorText.PreviewERPStatusText.Text = GetStatusText(Profile.Status);
        _flavorText.PreviewERPStatusText.FontColorOverride = GetStatusColor(Profile.Status);
    }

    private void CreateGYRBigTextLabel(string text, Color color)
    {
        var label = new Label
        {
            Text = text,
            VerticalExpand = true,
            StyleClasses = { StyleClass.LabelHeading },
            FontColorOverride = color
        };

        _flavorText?.PreviewGYRContainer.AddChild(label);
    }

    private void CreateGYRTextLabel(string text)
    {
        var label = new RichTextLabel
        {
            Text = text + "\n",
            VerticalExpand = true
        };

        _flavorText?.PreviewGYRContainer.AddChild(label);
    }

    private void ProcessLinks(string linksText)
    {
        if (_flavorText?.PreviewLinksContainer == null)
            return;

        _flavorText.PreviewLinksContainer.RemoveAllChildren();
        if (string.IsNullOrEmpty(linksText))
            return;

        var links = linksText.Split(new[] { ',', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var link in links)
        {
            if (IsValidUrl(link))
            {
                CreateLinkButton(link);
            }
            else
            {
                CreateLinkTextLabel(link);
            }
        }
    }

    private bool IsValidUrl(string url)
    {
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("www.", StringComparison.OrdinalIgnoreCase);
    }

    private void CreateLinkButton(string url)
    {
        var button = new Button
        {
            Text = GetLinkDisplayText(url),
            ToolTip = Loc.GetString("humanoid-profile-editor-link-tooltip", ("url", url)),
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            StyleClasses = { StyleClass.ButtonOpenBoth }
        };

        button.OnPressed += _ => OpenLink(url);

        _flavorText?.PreviewLinksContainer.AddChild(button);
    }

    private void CreateLinkTextLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            FontColorOverride = Color.Gray
        };

        _flavorText?.PreviewLinksContainer.AddChild(label);
    }

    private string GetLinkDisplayText(string url)
    {
        if (url.Length > 40)
        {
            return url[..37] + "...";
        }
        return url;
    }

    private void OpenLink(string url)
    {
        if (url.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        var uriOpener = IoCManager.Resolve<IUriOpener>();
        uriOpener.OpenUri(url);
    }

    private string GetStatusText(Status status)
    {
        return status switch
        {
            Status.No => Loc.GetString("humanoid-profile-editor-status-no-text"),
            Status.Semi => Loc.GetString("humanoid-profile-editor-status-semi-text"),
            Status.Full => Loc.GetString("humanoid-profile-editor-status-full-text"),
            Status.Absolute => Loc.GetString("humanoid-profile-editor-status-absolute-text"),
            _ => string.Empty
        };
    }

    private Color GetStatusColor(Status status)
    {
        return status switch
        {
            Status.No => Color.Red,
            Status.Semi => Color.Orange,
            Status.Full => Color.Blue,
            Status.Absolute => Color.Purple,
            _ => Color.Gray
        };
    }

    private void OnFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithFlavorText(content);
        SetDirty();
    }

    private void OnFlavorOOCTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithOOCFlavorText(content);
        SetDirty();
    }

    private void OnCharacterFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithCharacterText(content);
        SetDirty();
    }

    private void OnGreenFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithGreenPreferencesText(content);
        SetDirty();
    }

    private void OnYellowFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithYellowPreferencesText(content);
        SetDirty();
    }

    private void OnRedFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithRedPreferencesText(content);
        SetDirty();
    }

    private void OnTagsFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithTagsText(content);
        SetDirty();
    }

    private void OnLinksFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithLinksText(content);
        SetDirty();
    }

    private void OnNSFWFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithNSFWPreferencesText(content);
        SetDirty();
    }

    private void UpdateFlavorTextEdit()
    {
        if (_flavorTextEdit != null)
        {
            _flavorTextEdit.TextRope = new Rope.Leaf(Profile?.FlavorText ?? "");
        }

        if (_flavorTextOOCEdit != null)
        {
            _flavorTextOOCEdit.TextRope = new Rope.Leaf(Profile?.OOCFlavorText ?? "");
        }

        if (_characterTextEdit != null)
        {
            _characterTextEdit.TextRope = new Rope.Leaf(Profile?.CharacterFlavorText ?? "");
        }

        if (_greenTextEdit != null)
        {
            _greenTextEdit.TextRope = new Rope.Leaf(Profile?.GreenFlavorText ?? "");
        }

        if (_yellowTextEdit != null)
        {
            _yellowTextEdit.TextRope = new Rope.Leaf(Profile?.YellowFlavorText ?? "");
        }

        if (_redTextEdit != null)
        {
            _redTextEdit.TextRope = new Rope.Leaf(Profile?.RedFlavorText ?? "");
        }

        if (_tagsTextEdit != null)
        {
            _tagsTextEdit.TextRope = new Rope.Leaf(Profile?.TagsFlavorText ?? "");
        }

        if (_linksTextEdit != null)
        {
            _linksTextEdit.TextRope = new Rope.Leaf(Profile?.LinksFlavorText ?? "");
        }

        if (_nsfwTextEdit != null)
        {
            _nsfwTextEdit.TextRope = new Rope.Leaf(Profile?.NSFWFlavorText ?? "");
        }
    }
}
