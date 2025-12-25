using Content.Shared.Blood.Cult;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Select.Construct.UI
{
    public sealed class BloodConstructMenuUIController : UIController
    {
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private BloodConstructMenu? _menu;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<OpenConstructMenuEvent>(OnConstructMenuReceived);
        }

        private void OnConstructMenuReceived(OpenConstructMenuEvent args, EntitySessionEventArgs eventArgs)
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            var userEntity = _entityManager.GetEntity(args.Uid);

            if (session?.AttachedEntity.HasValue == true && session.AttachedEntity.Value == userEntity)
            {
                if (_menu is null)
                {
                    _menu = _uiManager.CreateWindow<BloodConstructMenu>();

                    _menu.SetData(args.ConstructUid, args.Mind);

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
    }
}
