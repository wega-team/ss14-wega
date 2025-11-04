using Content.Client.Select.Class.UI;
using Content.Shared.Vampire;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Select.Class
{
    public sealed class SelectClassUIController : UIController
    {
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private SelectClassMenu? _menu;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<SelectClassPressedEvent>(OnSelectClassMenuReceived);
        }

        private void OnSelectClassMenuReceived(SelectClassPressedEvent args, EntitySessionEventArgs eventArgs)
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            var userEntity = _entityManager.GetEntity(args.Uid);
            if (session?.AttachedEntity.HasValue == true && session.AttachedEntity.Value == userEntity)
            {
                if (_menu is null)
                {
                    _menu = _uiManager.CreateWindow<SelectClassMenu>();
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
