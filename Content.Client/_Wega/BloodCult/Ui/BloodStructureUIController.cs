using Content.Shared.Blood.Cult;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Structure.UI
{
    public sealed class BloodStructureMenuUIController : UIController
    {
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private BloodStructureMenu? _menu;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<OpenStructureMenuEvent>(OnStructureMenuReceived);
        }

        private void OnStructureMenuReceived(OpenStructureMenuEvent args, EntitySessionEventArgs eventArgs)
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            var userEntity = _entityManager.GetEntity(args.Uid);
            if (session?.AttachedEntity.HasValue == true && session.AttachedEntity.Value == userEntity)
            {
                if (_menu is null)
                {
                    _menu = _uiManager.CreateWindow<BloodStructureMenu>();
                    _menu.OnClose += OnMenuClosed;

                    _menu.SetData(args.Structure);

                    _menu.OpenCentered();
                }
                else
                {
                    _menu.OpenCentered();
                }

                Timer.Spawn(30000, () =>
                {
                    if (_menu != null)
                    {
                        _menu.Close();
                    }
                });
            }
        }

        private void OnMenuClosed()
        {
            _menu = null;
        }
    }
}
