using System.Linq;
using System.Numerics;
using Content.Client.Lobby.UI.Loadouts;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Stylesheets;
using Content.Shared.Clothing;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{

    /// <summary>
    /// Temporary override of their selected job, used to preview roles.
    /// </summary>
    public JobPrototype? JobOverride;

    // One at a time.
    private LoadoutWindow? _loadoutWindow;

    private List<(string, RequirementsSelector)> _jobPriorities = new();

    private readonly Dictionary<string, BoxContainer> _jobCategories;
    private readonly Dictionary<string, (Button Toggle, BoxContainer Container)> _subRoleContainers = new(); // Corvax-Wega-SubRoles

    /// <summary>
    /// Updates selected job priorities to the profile's.
    /// </summary>
    private void UpdateJobPriorities()
    {
        foreach (var (jobId, prioritySelector) in _jobPriorities)
        {
            var priority = Profile?.JobPriorities.GetValueOrDefault(jobId, JobPriority.Never) ?? JobPriority.Never;
            prioritySelector.Select((int)priority);
        }
    }

    /// <summary>
    /// Refresh all loadouts.
    /// </summary>
    public void RefreshLoadouts()
    {
        _loadoutWindow?.Dispose();
    }

    private void OpenLoadout(JobPrototype? jobProto, RoleLoadout roleLoadout, RoleLoadoutPrototype roleLoadoutProto)
    {
        _loadoutWindow?.Dispose();
        _loadoutWindow = null;
        var collection = IoCManager.Instance;

        if (collection == null || _playerManager.LocalSession == null || Profile == null)
            return;

        JobOverride = jobProto;
        var session = _playerManager.LocalSession;

        _loadoutWindow = new LoadoutWindow(Profile, roleLoadout, roleLoadoutProto, _playerManager.LocalSession, collection)
        {
            Title = Loc.GetString("loadout-window-title-loadout", ("job", $"{jobProto?.LocalizedName}")),
        };

        // Refresh the buttons etc.
        _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
        _loadoutWindow.OpenCenteredLeft();

        _loadoutWindow.OnNameChanged += name =>
        {
            roleLoadout.EntityName = name;
            Profile = Profile.WithLoadout(roleLoadout);
            SetDirty();
        };

        _loadoutWindow.OnLoadoutPressed += (loadoutGroup, loadoutProto) =>
        {
            roleLoadout.AddLoadout(loadoutGroup, loadoutProto, _prototypeManager);
            _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
            Profile = Profile?.WithLoadout(roleLoadout);
            ReloadPreview();
        };

        _loadoutWindow.OnLoadoutUnpressed += (loadoutGroup, loadoutProto) =>
        {
            roleLoadout.RemoveLoadout(loadoutGroup, loadoutProto, _prototypeManager);
            _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
            Profile = Profile?.WithLoadout(roleLoadout);
            ReloadPreview();
        };

        JobOverride = jobProto;
        ReloadPreview();

        _loadoutWindow.OnClose += () =>
        {
            JobOverride = null;
            ReloadPreview();
        };

        if (Profile is null)
            return;

        UpdateJobPriorities();
    }

    /// <summary>
    /// Refreshes all job selectors.
    /// </summary>
    public void RefreshJobs()
    {
        JobList.RemoveAllChildren();
        _jobCategories.Clear();
        _jobPriorities.Clear();
        _subRoleContainers.Clear(); // Corvax-Wega-SubRoles
        var firstCategory = true;

        // Get all displayed departments
        var departments = new List<DepartmentPrototype>();
        foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (department.EditorHidden)
                continue;

            departments.Add(department);
        }

        departments.Sort(DepartmentUIComparer.Instance);

        var items = new[]
        {
                ("humanoid-profile-editor-job-priority-never-button", (int) JobPriority.Never),
                ("humanoid-profile-editor-job-priority-low-button", (int) JobPriority.Low),
                ("humanoid-profile-editor-job-priority-medium-button", (int) JobPriority.Medium),
                ("humanoid-profile-editor-job-priority-high-button", (int) JobPriority.High),
            };

        foreach (var department in departments)
        {
            var departmentName = Loc.GetString(department.Name);

            if (!_jobCategories.TryGetValue(department.ID, out var category))
            {
                category = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Name = department.ID,
                    ToolTip = Loc.GetString("humanoid-profile-editor-jobs-amount-in-department-tooltip",
                        ("departmentName", departmentName))
                };

                if (firstCategory)
                {
                    firstCategory = false;
                }
                else
                {
                    category.AddChild(new Control
                    {
                        MinSize = new Vector2(0, 23),
                    });
                }

                category.AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#464966") },
                    Children =
                        {
                            new Label
                            {
                                Text = Loc.GetString("humanoid-profile-editor-department-jobs-label",
                                    ("departmentName", departmentName)),
                                Margin = new Thickness(5f, 0, 0, 0)
                            }
                        }
                });

                _jobCategories[department.ID] = category;
                JobList.AddChild(category);
            }

            // Corvax-Wega-SubRoles-Edit-start
            var jobs = department.Roles
                .Select(jobId => _prototypeManager.Index(jobId))
                .Where(job => job.SetPreference && !job.IsSubRole)
                .ToArray();
            // Corvax-Wega-SubRoles-Edit-end

            Array.Sort(jobs, JobUIComparer.Instance);

            foreach (var job in jobs)
            {
                // Corvax-Wega-SubRoles-Edit-start
                var jobContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                var mainRow = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal
                };
                // Corvax-Wega-SubRoles-Edit-end

                var selector = new RequirementsSelector()
                {
                    Margin = new Thickness(3f, 3f, 3f, 0f),
                };
                selector.OnOpenGuidebook += OnOpenGuidebook;

                var icon = new TextureRect
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center
                };
                var jobIcon = _prototypeManager.Index(job.Icon);
                icon.Texture = _sprite.Frame0(jobIcon.Icon);
                selector.Setup(items, job.LocalizedName, 200, job.LocalizedDescription, icon, job.Guides);

                if (!_requirements.IsAllowed(job, (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter, out var reason))
                {
                    selector.LockRequirements(reason);
                }
                else
                {
                    selector.UnlockRequirements();
                }

                selector.OnSelected += selectedPrio =>
                {
                    var selectedJobPrio = (JobPriority)selectedPrio;
                    Profile = Profile?.WithJobPriority(job.ID, selectedJobPrio);

                    foreach (var (jobId, other) in _jobPriorities)
                    {
                        // Sync other selectors with the same job in case of multiple department jobs
                        if (jobId == job.ID)
                        {
                            other.Select(selectedPrio);
                            continue;
                        }

                        if (selectedJobPrio != JobPriority.High || (JobPriority)other.Selected != JobPriority.High)
                            continue;

                        // Lower any other high priorities to medium.
                        other.Select((int)JobPriority.Medium);
                        Profile = Profile?.WithJobPriority(jobId, JobPriority.Medium);
                    }

                    // TODO: Only reload on high change (either to or from).
                    ReloadPreview();

                    UpdateJobPriorities();
                    SetDirty();
                };

                var loadoutWindowBtn = new Button()
                {
                    Text = Loc.GetString("loadout-window"),
                    HorizontalAlignment = HAlignment.Right,
                    VerticalAlignment = VAlignment.Center,
                    Margin = new Thickness(3f, 3f, 0f, 0f),
                };

                var collection = IoCManager.Instance!;
                var protoManager = collection.Resolve<IPrototypeManager>();

                // If no loadout found then disabled button
                if (!protoManager.TryIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID), out var roleLoadoutProto))
                {
                    loadoutWindowBtn.Disabled = true;
                }
                else
                {
                    loadoutWindowBtn.OnPressed += args =>
                    {
                        RoleLoadout? loadout = null;

                        // Clone so we don't modify the underlying loadout.
                        Profile?.Loadouts.TryGetValue(LoadoutSystem.GetJobPrototype(job.ID), out loadout);
                        loadout = loadout?.Clone();

                        if (loadout == null)
                        {
                            loadout = new RoleLoadout(roleLoadoutProto.ID);
                            loadout.SetDefault(Profile, _playerManager.LocalSession, _prototypeManager);
                        }

                        OpenLoadout(job, loadout, roleLoadoutProto);
                    };
                }

                _jobPriorities.Add((job.ID, selector));

                // Corvax-Wega-SubRoles-start
                mainRow.AddChild(selector);
                if (job.SubRoles.Count > 0)
                {
                    loadoutWindowBtn.StyleClasses.Add(StyleClass.ButtonOpenRight);
                }

                mainRow.AddChild(loadoutWindowBtn);

                // Add toggle button for subroles if they exist
                if (job.SubRoles.Count > 0)
                {
                    var toggleButton = new Button
                    {
                        Text = "▼",
                        HorizontalAlignment = HAlignment.Right,
                        ToolTip = Loc.GetString("humanoid-profile-editor-toggle-subroles"),
                        StyleClasses = { StyleClass.ButtonOpenLeft }
                    };

                    var subRolesContainer = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Visible = false,
                        Margin = new Thickness(20, 5, 0, 0)
                    };

                    toggleButton.OnPressed += _ =>
                    {
                        subRolesContainer.Visible = !subRolesContainer.Visible;
                        toggleButton.Text = subRolesContainer.Visible ? "▲" : "▼";
                    };

                    mainRow.AddChild(toggleButton);
                    jobContainer.AddChild(mainRow);
                    jobContainer.AddChild(subRolesContainer);

                    // Store reference to subroles container for potential future use
                    _subRoleContainers[job.ID] = (toggleButton, subRolesContainer);

                    // Add subroles
                    foreach (var subRoleId in job.SubRoles)
                    {
                        if (!_prototypeManager.TryIndex(subRoleId, out JobPrototype? subRole))
                            continue;

                        var subRow = new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            Margin = new Thickness(0, 0, 0, 5)
                        };

                        var subSelector = new RequirementsSelector()
                        {
                            Margin = new Thickness(3f, 3f, 3f, 0f),
                        };
                        subSelector.OnOpenGuidebook += OnOpenGuidebook;

                        var subIcon = new TextureRect
                        {
                            TextureScale = new Vector2(2, 2),
                            VerticalAlignment = VAlignment.Center
                        };
                        var subJobIcon = _prototypeManager.Index(subRole.Icon);
                        subIcon.Texture = _sprite.Frame0(subJobIcon.Icon);
                        subSelector.Setup(items, subRole.LocalizedName, 180, subRole.LocalizedDescription, subIcon, subRole.Guides);

                        if (!_requirements.IsAllowed(subRole, (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter, out var subReason))
                        {
                            subSelector.LockRequirements(subReason);
                        }
                        else
                        {
                            subSelector.UnlockRequirements();
                        }

                        subSelector.OnSelected += selectedPrio =>
                        {
                            Profile = Profile?.WithJobPriority(subRole.ID, (JobPriority)selectedPrio);
                            UpdateJobPriorities();
                            SetDirty();
                        };

                        var subLoadoutBtn = new Button()
                        {
                            Text = Loc.GetString("loadout-window"),
                            HorizontalAlignment = HAlignment.Right,
                            VerticalAlignment = VAlignment.Center,
                            Margin = new Thickness(3f, 3f, 0f, 0f),
                        };

                        if (!protoManager.TryIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(subRole.ID), out var subRoleLoadoutProto))
                        {
                            subLoadoutBtn.Disabled = true;
                        }
                        else
                        {
                            subLoadoutBtn.OnPressed += args =>
                            {
                                RoleLoadout? subLoadout = null;
                                Profile?.Loadouts.TryGetValue(LoadoutSystem.GetJobPrototype(subRole.ID), out subLoadout);
                                subLoadout = subLoadout?.Clone();

                                if (subLoadout == null)
                                {
                                    subLoadout = new RoleLoadout(subRoleLoadoutProto.ID);
                                    subLoadout.SetDefault(Profile, _playerManager.LocalSession, _prototypeManager);
                                }

                                OpenLoadout(subRole, subLoadout, subRoleLoadoutProto);
                            };
                        }

                        subRow.AddChild(subSelector);
                        subRow.AddChild(subLoadoutBtn);
                        subRolesContainer.AddChild(subRow);
                        _jobPriorities.Add((subRole.ID, subSelector));
                    }
                }
                else
                {
                    jobContainer.AddChild(mainRow);
                }

                category.AddChild(jobContainer);
                // Corvax-Wega-SubRoles-end
            }
        }

        UpdateJobPriorities();
    }

    public void RefreshAntags()
    {
        AntagList.RemoveAllChildren();
        var items = new[]
        {
            ("humanoid-profile-editor-antag-preference-yes-button", 0),
            ("humanoid-profile-editor-antag-preference-no-button", 1)
        };

        foreach (var antag in _prototypeManager.EnumeratePrototypes<AntagPrototype>().OrderBy(a => Loc.GetString(a.Name)))
        {
            if (!antag.SetPreference)
                continue;

            var antagContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
            };

            var selector = new RequirementsSelector()
            {
                Margin = new Thickness(3f, 3f, 3f, 0f),
            };
            selector.OnOpenGuidebook += OnOpenGuidebook;

            var title = Loc.GetString(antag.Name);
            var description = Loc.GetString(antag.Objective);
            selector.Setup(items, title, 250, description, guides: antag.Guides);
            selector.Select(Profile?.AntagPreferences.Contains(antag.ID) == true ? 0 : 1);

            if (!_requirements.IsAllowed(
                    antag,
                    (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter,
                    out var reason))
            {
                selector.LockRequirements(reason);
                Profile = Profile?.WithAntagPreference(antag.ID, false);
                SetDirty();
            }
            else
            {
                selector.UnlockRequirements();
            }

            selector.OnSelected += preference =>
            {
                Profile = Profile?.WithAntagPreference(antag.ID, preference == 0);
                SetDirty();
            };

            antagContainer.AddChild(selector);

            antagContainer.AddChild(new Button()
            {
                Disabled = true,
                Text = Loc.GetString("loadout-window"),
                HorizontalAlignment = HAlignment.Right,
                Margin = new Thickness(3f, 0f, 0f, 0f),
            });

            AntagList.AddChild(antagContainer);
        }
    }
}
