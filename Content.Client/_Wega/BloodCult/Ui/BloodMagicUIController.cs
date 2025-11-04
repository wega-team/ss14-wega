using Content.Shared.Blood.Cult;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Blood.Magic.UI
{
    public sealed class BloodMagicMenuUIController : UIController
    {
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private BloodMagicMenu? _menu;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<BloodMagicPressedEvent>(OnBloodMagicMenuReceived);
        }

        private void OnBloodMagicMenuReceived(BloodMagicPressedEvent args, EntitySessionEventArgs eventArgs)
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            var userEntity = _entityManager.GetEntity(args.Uid);
            if (session?.AttachedEntity.HasValue == true && session.AttachedEntity.Value == userEntity)
            {
                if (_menu is null)
                {
                    _menu = _uiManager.CreateWindow<BloodMagicMenu>();
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
