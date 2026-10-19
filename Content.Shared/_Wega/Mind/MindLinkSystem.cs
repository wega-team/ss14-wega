using JetBrains.Annotations;

namespace Content.Shared.Mind;

public sealed partial class MindLinkSystem : EntitySystem
{
    /// <summary>
    /// Adds a channel. Creates the component if it doesn't exist yet.
    /// </summary>
    [PublicAPI]
    public void AddChannel(EntityUid uid, string channel)
    {
        if (string.IsNullOrEmpty(channel))
            return;

        var mindLink = EnsureComp<MindLinkComponent>(uid);
        if (mindLink.Channels.Contains(channel))
            return;

        mindLink.Channels.Add(channel);
        Dirty(uid, mindLink);
    }

    /// <summary>
    /// Removes a channel. If no channels remain, removes the component.
    /// </summary>
    [PublicAPI]
    public void RemoveChannel(Entity<MindLinkComponent?> ent, string channel)
    {
        if (!Resolve(ent, ref ent.Comp, false) || string.IsNullOrEmpty(channel))
            return;

        if (!ent.Comp.Channels.Remove(channel))
            return;

        if (ent.Comp.Channels.Count == 0)
        {
            RemComp<MindLinkComponent>(ent);
            return;
        }

        Dirty(ent.Owner, ent.Comp);
    }

    /// <summary>
    /// Checks whether the entity has the given channel.
    /// </summary>
    [PublicAPI]
    public bool HasChannel(EntityUid? uid, string channel)
    {
        return uid is { } ent && HasChannel(ent, channel);
    }

    /// <summary>
    /// Checks whether the entity has the given channel.
    /// </summary>
    [PublicAPI]
    public bool HasChannel(Entity<MindLinkComponent?> ent, string channel)
    {
        if (!Resolve(ent, ref ent.Comp, false) || string.IsNullOrEmpty(channel))
            return false;

        return ent.Comp.Channels.Contains(channel);
    }
}
