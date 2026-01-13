using Robust.Shared.Serialization.Manager;
using Content.Shared.Cloning.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles removing accents when using the accentless trait.
/// </summary>
public sealed class AccentlessSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccentlessComponent, ComponentStartup>(RemoveAccents);
		SubscribeLocalEvent<AccentlessComponent, CloneFinishedEvent>(OnCloneFinished);
    }

    private void RemoveAccents(EntityUid uid, AccentlessComponent component, ComponentStartup args)
    {
        foreach (var accent in component.RemovedAccents.Values)
        {
            var accentComponent = accent.Component;
            RemComp(uid, accentComponent.GetType());
        }
    }
	
	private void OnCloneFinished(EntityUid uid, AccentlessComponent component, CloneFinishedEvent args)
    {
        RemoveAccents(uid, component, new ComponentStartup());
	}
}

