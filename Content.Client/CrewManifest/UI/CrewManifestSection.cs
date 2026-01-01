using Content.Shared.CrewManifest;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.Graphics; // Corvax-Wega-Add
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility; // Corvax-Wega-Add
using System.Numerics;
using Content.Shared.Roles;

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestSection : BoxContainer
{
    public CrewManifestSection(
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        DepartmentPrototype section,
        List<CrewManifestEntry> entries)
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        // Corvax-Wega-Edit-start
        AddChild(new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#2D2D33"),
                ContentMarginBottomOverride = 2,
                ContentMarginLeftOverride = 4,
                ContentMarginRightOverride = 4,
                ContentMarginTopOverride = 2
            },
            Children =
            {
                new Label()
                {
                    StyleClasses = { "LabelBig" },
                    Text = Loc.GetString(section.Name),
                    Margin = new Thickness(4, 2)
                }
            }
        });

        var departmentContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(0, 4, 0, 8)
        };

        AddChild(departmentContainer);

        var rowCount = 0;
        foreach (var entry in entries)
        {
            var rowColor1 = Color.FromHex("#1B1B1E");
            var rowColor2 = Color.FromHex("#202025");
            var currentRowColor = (rowCount % 2 == 0) ? rowColor1 : rowColor2;
            rowCount++;

            var rowPanel = new PanelContainer
            {
                HorizontalExpand = true,
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = currentRowColor,
                    ContentMarginBottomOverride = 4,
                    ContentMarginLeftOverride = 8,
                    ContentMarginRightOverride = 8,
                    ContentMarginTopOverride = 4
                }
            };

            var rowContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
                SeparationOverride = 8
            };

            var nameContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                HorizontalExpand = true,
                VerticalExpand = true
            };

            var name = new RichTextLabel()
            {
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
                Margin = new Thickness(4, 0)
            };
            name.SetMessage(entry.Name);
            nameContainer.AddChild(name);

            var titleContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center
            };

            var title = new RichTextLabel()
            {
                VerticalAlignment = VAlignment.Center
            };
            title.SetMessage(entry.JobTitle);

            SpriteSpecifier? iconSpecifier = null;
            if (prototypeManager.HasIndex<JobIconPrototype>(entry.JobIcon))
            {
                var jobIcon = prototypeManager.Index<JobIconPrototype>(entry.JobIcon);
                iconSpecifier = jobIcon.Icon;
            }
            else if (prototypeManager.HasIndex<StatusIconPrototype>(entry.JobIcon))
            {
                var statusIcon = prototypeManager.Index<StatusIconPrototype>(entry.JobIcon);
                iconSpecifier = statusIcon.Icon;
            }

            if (iconSpecifier != null)
            {
                var icon = new TextureRect()
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center,
                    HorizontalAlignment = HAlignment.Left,
                    Texture = spriteSystem.Frame0(iconSpecifier),
                    Margin = new Thickness(0, 0, 4, 0),
                    Stretch = TextureRect.StretchMode.KeepCentered
                };

                titleContainer.AddChild(icon);
            }

            titleContainer.AddChild(title);

            rowContainer.AddChild(nameContainer);
            rowContainer.AddChild(titleContainer);

            rowPanel.AddChild(rowContainer);

            departmentContainer.AddChild(rowPanel);
            // Corvax-Wega-Edit-end
        }
    }
}
