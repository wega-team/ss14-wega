using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;

namespace Content.Shared.Strangulation
{
    public sealed class SharedStrangulationSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StrangulationComponent, UpdateCanMoveEvent>(HandleMovementBlock);
            SubscribeLocalEvent<StrangulationComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<StrangulationComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnStartup(EntityUid uid, StrangulationComponent component, ComponentStartup args)
        {
            _actionBlockerSystem.UpdateCanMove(uid);
        }

        private void OnShutdown(EntityUid uid, StrangulationComponent component, ComponentShutdown args)
        {
            _actionBlockerSystem.UpdateCanMove(uid);
        }

        private void HandleMovementBlock(EntityUid uid, StrangulationComponent component, UpdateCanMoveEvent args)
        {
            if (component.Cancelled)
                return;
            args.Cancel();
        }
    }
}
