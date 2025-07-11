
using Content.Shared.Actions;
using Content.Shared.Martial.Arts.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Martial.Arts;

public abstract class SharedMartialArtsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MartialArtsComponent, MapInitEvent>(OnInitialized);
        SubscribeLocalEvent<MartialArtsComponent, ComponentRemove>(OnRemoved);
    }

    private void OnInitialized(Entity<MartialArtsComponent> ent, ref MapInitEvent args)
    {
        if (!_prototype.TryIndex(ent.Comp.Style, out var stylePrototype))
            return;

        if (stylePrototype.Actions == null)
            return;

        foreach (var action in stylePrototype.Actions)
        {
            var newAction = _action.AddAction(ent, action);
            if (newAction != null)
            {
                ent.Comp.AddedActions.Add(newAction.Value);
            }
        }
    }

    private void OnRemoved(Entity<MartialArtsComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.AddedActions == null)
            return;

        foreach (var action in ent.Comp.AddedActions)
        {
            _action.RemoveAction(ent.Owner, action);
        }
    }

    /// <summary>
    /// A method for adding a martial art.
    /// </summary>
    /// <param name="uid">The entity to which the component is added.</param>
    /// <param name="style">The style that needs to be assigned.</param>
    /// <returns></returns>
    public bool TryAddMartialArts(EntityUid uid, string style)
    {
        if (!HasComp<MartialArtsComponent>(uid))
        {
            EnsureComp<MartialArtsComponent>(uid, out var comp);

            comp.Style = style;
            Dirty(uid, comp);
            return true;
        }
        return false;
    }
}
