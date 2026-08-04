using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Robust.Shared.Containers;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Allows the ninja headset to copy encryption channels from other headsets by clicking on them.
/// Keys inside are locked — they cannot be inserted or removed through normal means.
/// </summary>
public sealed partial class NinjaHeadsetSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private const string CopiedKeyProto = "EncryptionKeyNinja";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaHeadsetComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<NinjaHeadsetComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!TryComp<EncryptionKeyHolderComponent>(args.Target, out var targetHolder))
            return;

        if (!TryComp<EncryptionKeyHolderComponent>(ent.Owner, out var ninjaHolder))
            return;

        args.Handled = true;

        var newChannels = targetHolder.Channels
            .Except(ninjaHolder.Channels)
            .ToHashSet();

        if (newChannels.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("ninja-headset-no-new-channels"), ent.Owner, args.User);
            return;
        }

        if (ninjaHolder.KeyContainer.ContainedEntities.Count >= ninjaHolder.KeySlots)
        {
            _popup.PopupEntity(Loc.GetString("ninja-headset-key-slots-full"), ent.Owner, args.User);
            return;
        }

        var key = Spawn(CopiedKeyProto, Transform(ent.Owner).Coordinates);
        Comp<EncryptionKeyComponent>(key).Channels.UnionWith(newChannels);

        _container.Insert(key, ninjaHolder.KeyContainer);
        _popup.PopupEntity(Loc.GetString("ninja-headset-key-copied"), ent.Owner, args.User);
    }
}
