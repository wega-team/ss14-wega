using Content.Shared.Blood.Cult;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Blood.Rites.UI
{
    public sealed class BloodRitesMenuUIController : UIController
    {
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private BloodRitesMenu? _menu;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<BloodRitesPressedEvent>(OnBloodMagicMenuReceived);
        }

        private void OnBloodMagicMenuReceived(BloodRitesPressedEvent args, EntitySessionEventArgs eventArgs)
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            var userEntity = _entityManager.GetEntity(args.Uid);
            if (session?.AttachedEntity.HasValue == true && session.AttachedEntity.Value == userEntity)
            {
                if (_menu is null)
                {
                    _menu = _uiManager.CreateWindow<BloodRitesMenu>();
                    _menu.OnClose += OnMenuClosed;
                    _menu.OpenCentered();
                }
                else
                {
                    _menu.OpenCentered();
                }
            }
        }

        private void OnMenuClosed()
        {
            _menu = null;
        }
    }
}
