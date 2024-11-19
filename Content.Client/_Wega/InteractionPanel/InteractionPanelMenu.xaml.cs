using System.Linq;
using System.Numerics;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Interaction.Panel.Ui
{
    public sealed partial class InteractionPanelMenu : DefaultWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IEntityNetworkManager _entityNetworkManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private BoxContainer _names;
        private BoxContainer _upperBlock;
        private BoxContainer _userModel;
        private BoxContainer _targetModel;
        private BoxContainer _interactionContainer;
        private SpriteView _userSpriteView;
        private SpriteView _targetSpriteView;
        private SpriteSystem _spriteSystem;
        private Label _targetLabel = new Label();
        private Label _userGenderLabel = new Label();
        private Label _targetGenderLabel = new Label();
        private LineEdit _searchBar;

        public InteractionPanelMenu()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            _spriteSystem = _entManager.System<SpriteSystem>();

            _names = this.FindControl<BoxContainer>("Names");
            _upperBlock = this.FindControl<BoxContainer>("UpperBlock");
            _userModel = this.FindControl<BoxContainer>("UserSpriteView");
            _targetModel = this.FindControl<BoxContainer>("TargetSpriteView");
            _interactionContainer = this.FindControl<BoxContainer>("InteractionContainer");
            _searchBar = this.FindControl<LineEdit>("SearchBar");
            _searchBar.OnTextChanged += OnSearchTextChanged;

            InitializeNamesContainer();

            _userSpriteView = CreateSpriteView();
            _userModel.AddChild(_userSpriteView);
            _userGenderLabel = CreateLabel("");
            _userModel.AddChild(_userGenderLabel);
            _targetGenderLabel = CreateLabel("");
            _targetModel.AddChild(_targetGenderLabel);
            _targetSpriteView = CreateSpriteView();
            _targetModel.AddChild(_targetSpriteView);

            _playerManager.PlayerStatusChanged += OnUserStatusChanged;

            PopulateInteractions();
            UpdateTarget();
        }

        private void InitializeNamesContainer()
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            if (session?.AttachedEntity.HasValue == true)
            {
                var user = session.AttachedEntity.Value;
                var appearanceComponent = _entManager.TryGetComponent<HumanoidAppearanceComponent>(user, out var appearance) ? appearance : null;

                _names.AddChild(CreateLabel(Loc.GetString("interact-player")));

                var userGenderIcon = CreateGenderIconButton(appearanceComponent?.Sex);
                if (userGenderIcon != null)
                    _names.AddChild(userGenderIcon);
            }

            _names.AddChild(CreateSpacer());

            _targetLabel = CreateLabel("");
            _names.AddChild(_targetLabel);
        }

        private TextureButton CreateGenderIconButton(Sex? sex)
        {
            string texturePath;

            switch (sex)
            {
                case Sex.Male:
                    texturePath = "/Textures/_Wega/Interface/InteractionPanel/male.png";
                    break;
                case Sex.Female:
                    texturePath = "/Textures/_Wega/Interface/InteractionPanel/female.png";
                    break;
                case Sex.Unsexed:
                    texturePath = "/Textures/_Wega/Interface/InteractionPanel/unsexed.png";
                    break;
                default:
                    texturePath = "/Textures/_Wega/Interface/InteractionPanel/unknown.png";
                    break;
            }

            var textureResource = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(new ResPath(texturePath));
            var texture = textureResource.Texture;

            return new TextureButton
            {
                TextureNormal = texture,
                Margin = new Thickness(4),
                Scale = new Vector2(0.5f, 0.5f)
            };
        }

        private SpriteView CreateSpriteView()
        {
            return new SpriteView
            {
                OverrideDirection = Direction.South,
                Scale = new Vector2(2, 2),
                SetSize = new Vector2(64, 64)
            };
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                Margin = new Thickness(4)
            };
        }

        private BoxContainer CreateSpacer()
        {
            return new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                MinHeight = 4,
                HorizontalExpand = true,
                Margin = new Thickness(0)
            };
        }

        private void UpdateTarget()
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;

            if (session?.AttachedEntity.HasValue == true)
            {
                var target = FindTarget(session.AttachedEntity.Value);

                if (target.HasValue)
                {
                    var targetEntityUid = _entManager.GetEntity(target.Value);
                    _targetSpriteView.SetEntity(target.Value);

                    if (_entManager.TryGetComponent<MetaDataComponent>(targetEntityUid, out var metaData))
                        _targetLabel.Text = Identity.Name(targetEntityUid, _entManager, session.AttachedEntity);

                    var appearanceComponent = _entManager.TryGetComponent<HumanoidAppearanceComponent>(targetEntityUid, out var appearance) ? appearance : null;
                    UpdateGender(appearanceComponent, _targetGenderLabel);
                }
                else
                {
                    _targetSpriteView.SetEntity(EntityUid.Invalid);
                    _targetGenderLabel.Text = "";
                    _targetLabel.Text = "";
                }
            }
        }

        private void UpdateGender(HumanoidAppearanceComponent? appearanceComponent, Label genderLabel)
        {
            if (appearanceComponent != null)
            {
                var species = appearanceComponent.Species;
                switch (species)
                {
                    case "Diona":
                        genderLabel.Text = string.Join("\n",
                            Loc.GetString("diona-leaves"),
                            Loc.GetString("diona-branches"));
                        break;
                    case "Arachnid":
                        genderLabel.Text = Loc.GetString("arachnid-nearestplayer");
                        break;
                    case "Vox":
                        genderLabel.Text = Loc.GetString("vox-nearestplayer");
                        break;
                    default:
                        switch (appearanceComponent.Sex)
                        {
                            case Sex.Male:
                                genderLabel.Text = string.Join("\n",
                                    Loc.GetString("male-anal"),
                                    Loc.GetString("male-dick"));
                                break;
                            case Sex.Female:
                                genderLabel.Text = string.Join("\n",
                                    Loc.GetString("female-anal"),
                                    Loc.GetString("female-vagine"),
                                    Loc.GetString("female-breasts"));
                                break;
                            case Sex.Unsexed:
                                genderLabel.Text = Loc.GetString("unsexed-nearestplayer");
                                break;
                            default:
                                genderLabel.Text = Loc.GetString("unknown-nearestplayer");
                                break;
                        }
                        break;
                }
                var targetGenderIcon = CreateGenderIconButton(appearanceComponent.Sex);
                if (targetGenderIcon != null)
                {
                    _names.AddChild(targetGenderIcon);
                }
            }
            else
            {
                genderLabel.Text = Loc.GetString("unknown-nearestplayer");
            }
        }

        private void OnUserStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.Session.AttachedEntity.HasValue)
            {
                UpdateUser(e.Session.AttachedEntity.Value);
                UpdateTarget();
            }
        }

        public void UpdateUser(EntityUid user)
        {
            _userSpriteView.SetEntity(user);

            var appearanceComponent = _entManager.TryGetComponent<HumanoidAppearanceComponent>(user, out var appearance) ? appearance : null;
            if (appearanceComponent != null)
            {
                var species = appearanceComponent.Species;
                switch (species)
                {
                    case "Diona":
                        _userGenderLabel.Text = string.Join("\n",
                            Loc.GetString("diona-leaves-player"),
                            Loc.GetString("diona-branches-player"));
                        break;
                    case "Arachnid":
                        _userGenderLabel.Text = Loc.GetString("arachnid-player");
                        break;
                    case "Vox":
                        _userGenderLabel.Text = Loc.GetString("vox-player");
                        break;
                    default:
                        switch (appearanceComponent.Sex)
                        {
                            case Sex.Male:
                                _userGenderLabel.Text = string.Join("\n",
                                    Loc.GetString("male-anal-player"),
                                    Loc.GetString("male-dick-player"));
                                break;
                            case Sex.Female:
                                _userGenderLabel.Text = string.Join("\n",
                                    Loc.GetString("female-anal-player"),
                                    Loc.GetString("female-vagine-player"),
                                    Loc.GetString("female-breasts-player"));
                                break;
                            case Sex.Unsexed:
                                _userGenderLabel.Text = Loc.GetString("unsexed-player");
                                break;
                            default:
                                _userGenderLabel.Text = Loc.GetString("unknown-player");
                                break;
                        }
                        break;
                }
            }
            else
                _userGenderLabel.Text = Loc.GetString("unknown-player");
        }

        private void PopulateInteractions()
        {
            var interactionPrototypes = _prototypeManager.EnumeratePrototypes<InteractionPrototype>();
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;

            if (session == null || !session.AttachedEntity.HasValue)
                return;

            var user = session.AttachedEntity.Value;
            if (!_entManager.TryGetComponent<HumanoidAppearanceComponent>(user, out var appearanceComponent))
                return;

            HumanoidAppearanceComponent? nearestAppearance = null;

            foreach (var prototype in interactionPrototypes)
            {
                var target = FindTarget(user);

                if (target.HasValue)
                {
                    var targetEntityUid = _entManager.GetEntity(target.Value);

                    if (!_entManager.TryGetComponent<HumanoidAppearanceComponent>(targetEntityUid, out var nearestAppearanceComponent))
                        continue;

                    if (prototype.TargetEntityId != null && !prototype.TargetEntityId.Contains(target.Value.ToString()))
                        continue;

                    nearestAppearance = nearestAppearanceComponent;
                }

                bool isSpeciesAllowed = prototype.AllowedSpecies?.Contains("all") == true ||
                    (appearanceComponent != null && prototype.AllowedSpecies?.Contains(appearanceComponent.Species) == true);

                bool isGenderAllowed = prototype.AllowedGenders?.Contains("all") == true ||
                    (appearanceComponent != null && prototype.AllowedGenders?.Contains(appearanceComponent.Sex.ToString()) == true);

                bool isNearestSpeciesAllowed = prototype.NearestAllowedSpecies?.Contains("all") == true ||
                    (nearestAppearance != null && prototype.NearestAllowedSpecies?.Contains(nearestAppearance.Species) == true);

                bool isNearestGenderAllowed = prototype.NearestAllowedGenders?.Contains("all") == true ||
                    (nearestAppearance != null && prototype.NearestAllowedGenders?.Contains(nearestAppearance.Sex.ToString()) == true);

                bool isTargetEntityAllowed = prototype.TargetEntityId == null ||
                    (target.HasValue && prototype.TargetEntityId.Contains(target.Value.ToString()));

                var isErpAllowed = prototype.ERP && appearanceComponent != null && appearanceComponent.Status != Status.No &&
                    _entManager.TryGetComponent(user, out HumanoidAppearanceComponent? userAppearanceComponent) &&
                    userAppearanceComponent?.Status != Status.No;

                if (!isSpeciesAllowed
                    || !isGenderAllowed
                    || !isNearestSpeciesAllowed
                    || !isNearestGenderAllowed
                    || !isTargetEntityAllowed
                    || (prototype.ERP && !isErpAllowed))
                    continue;

                var buttonContainer = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    HorizontalExpand = false,
                    Margin = new Thickness(1)
                };

                var button = new Button
                {
                    Text = Loc.GetString(prototype.Name),
                    MinWidth = 420,
                    MinHeight = 32
                };

                button.OnButtonDown += args => OnInteractionPressed(prototype.ID);

                var iconSpecifier = prototype.Icon;
                if (iconSpecifier is SpriteSpecifier.Texture textureSpecifier)
                {
                    var texturePath = textureSpecifier.TexturePath;

                    if (!texturePath.IsRooted)
                        texturePath = new ResPath("/Textures/" + texturePath);

                    var textureResource = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(texturePath);
                    var texture = textureResource.Texture;

                    var iconButton = new TextureButton
                    {
                        TextureNormal = texture,
                        Margin = new Thickness(4),
                        Scale = new Vector2(1f, 1f)
                    };

                    buttonContainer.AddChild(iconButton);
                }

                buttonContainer.AddChild(button);
                _interactionContainer.AddChild(buttonContainer);
            }
        }

        private void OnSearchTextChanged(LineEdit.LineEditEventArgs args)
        {
            var searchText = _searchBar.Text.ToLower();

            foreach (var child in _interactionContainer.Children)
            {
                if (child is BoxContainer container)
                {
                    var button = container.Children.OfType<Button>().FirstOrDefault();
                    var iconButton = container.Children.OfType<TextureButton>().FirstOrDefault();

                    if (button != null && button.Text != null && button.Text.ToLower().Contains(searchText))
                    {
                        button.Visible = true;
                        if (iconButton != null)
                            iconButton.Visible = true;
                    }
                    else
                    {
                        if (button != null)
                            button.Visible = false;
                        if (iconButton != null)
                            iconButton.Visible = false;
                    }
                }
            }
        }

        private void OnInteractionPressed(string interactionId)
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            if (session?.AttachedEntity.HasValue == true)
            {
                var user = session.AttachedEntity.Value;

                if (_entManager.TryGetNetEntity(user, out var userEntity) && userEntity.HasValue)
                {
                    var target = FindTarget(user);

                    _entityNetworkManager.SendSystemNetworkMessage(new InteractionPressedEvent(userEntity.Value, interactionId, target));
                }
            }
        }

        private NetEntity? FindTarget(EntityUid user)
        {
            const float maxDistance = 2f;
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            NetEntity? target = null;
            float minDistance = float.MaxValue;

            if (!_entManager.TryGetComponent<TransformComponent>(user, out var sourceTransform))
                return null;

            foreach (var sessionUser in playerManager.Sessions)
            {
                if (sessionUser.AttachedEntity is not EntityUid userEntity || userEntity == user)
                    continue;

                if (!_entManager.TryGetComponent<TransformComponent>(userEntity, out var userTransform))
                    continue;

                var distance = Vector2.Distance(sourceTransform.Coordinates.Position, userTransform.Coordinates.Position);

                if (distance <= maxDistance && distance < minDistance)
                {
                    minDistance = distance;
                    target = _entManager.GetNetEntity(userEntity);
                }
            }

            return target;
        }
    }
}
