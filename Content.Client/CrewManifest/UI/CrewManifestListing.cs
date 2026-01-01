using System.Linq; // Corvax-Wega-Add
using System.Numerics; // Corvax-Wega-Add
using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface; // Corvax-Wega-Add
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestListing : BoxContainer
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly SpriteSystem _spriteSystem;

    public CrewManifestListing()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
        // Corvax-Wega-Add-start
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;
        SeparationOverride = 4;
        // Corvax-Wega-Add-end
    }

    public void AddCrewManifestEntries(CrewManifestEntries entries)
    {
        // Corvax-Wega-Add-start
        RemoveAllChildren();

        if (entries == null || entries.Entries.Count() == 0)
        {
            AddChild(new Label
            {
                Text = Loc.GetString("crew-manifest-no-entries"),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                HorizontalExpand = true,
                VerticalExpand = true
            });
            return;
        }
        // Corvax-Wega-Add-end

        var entryDict = new Dictionary<DepartmentPrototype, List<CrewManifestEntry>>();

        foreach (var entry in entries.Entries)
        {
            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                // this is a little expensive, and could be better
                if (department.Roles.Contains(entry.JobPrototype))
                {
                    entryDict.GetOrNew(department).Add(entry);
                }
            }
        }

        var entryList = new List<(DepartmentPrototype section, List<CrewManifestEntry> entries)>();

        foreach (var (section, listing) in entryDict)
        {
            entryList.Add((section, listing));
        }

        entryList.Sort((a, b) => DepartmentUIComparer.Instance.Compare(a.section, b.section));

        // Corvax-Wega-Edit-start
        foreach (var item in entryList)
        {
            var section = new CrewManifestSection(_prototypeManager, _spriteSystem, item.section, item.entries);
            section.Margin = new Thickness(0, 0, 0, 8);
            AddChild(section);
        }

        if (entryList.Count > 0)
        {
            AddChild(new Control { MinSize = new Vector2(0, 4) });
        }
        // Corvax-Wega-Edit-start
    }
}

