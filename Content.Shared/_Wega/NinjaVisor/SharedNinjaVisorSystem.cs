using Content.Shared.Actions;

namespace Content.Shared._Wega.NinjaVisor;

public sealed partial class SharedNinjaVisorSystem : EntitySystem
{
    [Dependency] private ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaVisorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NinjaVisorComponent, GetItemActionsEvent>(OnGetItemActions);
    }

    private void OnMapInit(Entity<NinjaVisorComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent.Owner, ref ent.Comp.CycleActionEntity, ent.Comp.CycleAction);
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnGetItemActions(Entity<NinjaVisorComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ent.Comp.CycleActionEntity);
    }
}
