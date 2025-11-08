using Content.Shared.Blood.Cult;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Runes.Panel.Ui
{
    public sealed class RunesMenuUIController : UIController
    {
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private RunesPanelMenu? _panel;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RunesMenuOpenedEvent>(OnRunesMenuReceived);
        }

        private void OnRunesMenuReceived(RunesMenuOpenedEvent args, EntitySessionEventArgs eventArgs)
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            var userEntity = _entityManager.GetEntity(args.Uid);
            if (session?.AttachedEntity.HasValue == true && session.AttachedEntity.Value == userEntity)
            {
                if (_panel is null)
                {
                    _panel = _uiManager.CreateWindow<RunesPanelMenu>();
                    _panel.OnClose += OnMenuClosed;
                    _panel.OpenCentered();
                }
                else
                {
                    _panel.OpenCentered();
                }
            }
        }

        private void OnMenuClosed()
        {
            _panel = null;
        }
    }

    public sealed class EmpoweringRuneMenuUIController : UIController
    {
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private EmpoweringRuneMenu? _menu;
        private bool _menuDisposed = false;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<EmpoweringRuneMenuOpenedEvent>(OnRuneMenuReceived);
        }

        private void OnRuneMenuReceived(EmpoweringRuneMenuOpenedEvent args, EntitySessionEventArgs eventArgs)
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            var userEntity = _entityManager.GetEntity(args.Uid);
            if (session?.AttachedEntity.HasValue == true && session.AttachedEntity.Value == userEntity)
            {
                if (_menu is null)
                {
                    _menu = _uiManager.CreateWindow<EmpoweringRuneMenu>();
                    _menu.OnClose += OnMenuClosed;
                    _menu.OpenCentered();
                }
                else
                {
                    _menu.OpenCentered();
                }
            }

            Timer.Spawn(30000, () =>
            {
                if (_menu != null && !_menuDisposed)
                {
                    _menu.Close();
                }
            });
        }

        private void OnMenuClosed()
        {
            _menuDisposed = true;
            _menu = null;
        }

    }

    public sealed class SummoningRuneMenuUIController : UIController
    {
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private SummoningRunePanelMenu? _panel;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<SummoningRuneMenuOpenedEvent>(OnRuneMenuReceived);
        }

        private void OnRuneMenuReceived(SummoningRuneMenuOpenedEvent args, EntitySessionEventArgs eventArgs)
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            var userEntity = _entityManager.GetEntity(args.Uid);
            if (session?.AttachedEntity.HasValue == true && session.AttachedEntity.Value == userEntity)
            {
                if (_panel is null)
                {
                    _panel = _uiManager.CreateWindow<SummoningRunePanelMenu>();
                    _panel.OnClose += OnMenuClosed;
                    _panel.OpenCentered();
                }
                else
                {
                    _panel.OpenCentered();
                }

                Timer.Spawn(30000, () =>
                {
                    if (_panel != null)
                    {
                        _panel.Close();
                    }
                });
            }
        }

        private void OnMenuClosed()
        {
            _panel = null;
        }
    }
}
