using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Veil.Cult
{
    public sealed class VeilCultSystem : EntitySystem
    {

        [Dependency] private readonly IPrototypeManager _prototype = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VeilCultistComponent, GetStatusIconsEvent>(GetCultistIcons);
        }

        private void GetCultistIcons(Entity<VeilCultistComponent> ent, ref GetStatusIconsEvent args)
        {
            var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
            args.StatusIcons.Add(iconPrototype);
        }
    }
}
