using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

public sealed partial class ActionTeleportShadekin : InstantActionEvent
{
}

public sealed partial class ActionCloseTeleportShadekin : InstantActionEvent
{
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActionTeleporterComponent : Component
{
    // Currently spawned portals, if any.
    [DataField, AutoNetworkedField]
    public EntityUid? FirstPortal;

    [DataField, AutoNetworkedField]
    public EntityUid? SecondPortal;

    // Time at which the first portal auto-closes if the second one was never placed.
    [DataField, AutoNetworkedField]
    public TimeSpan? FirstPortalDeadline;

    // Time at which both linked portals auto-close.
    [DataField, AutoNetworkedField]
    public TimeSpan? PortalsCloseDeadline;

    // Action entities granted to the owner at each stage.
    [DataField, AutoNetworkedField]
    public EntityUid? CreatePortalActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? CreateSecondPortalActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? ClosePortalsActionEntity;

    // How long the owner has to place the second portal after the first one.
    [DataField]
    public TimeSpan FirstPortalTimeout = TimeSpan.FromSeconds(30);

    // How long a linked pair of portals stays open before auto-closing.
    [DataField]
    public TimeSpan PortalsCloseTimeout = TimeSpan.FromSeconds(120);

    // Cooldown applied when only one portal was placed.
    [DataField]
    public TimeSpan OnePortalCooldown = TimeSpan.FromSeconds(60);

    // Cooldown applied when both portals were placed.
    [DataField]
    public TimeSpan TwoPortalCooldown = TimeSpan.FromSeconds(300);

    // Prototypes used to spawn the portals themselves.
    [DataField]
    public EntProtoId FirstPortalPrototype = "PortalRed";

    [DataField]
    public EntProtoId SecondPortalPrototype = "PortalBlue";

    // Prototypes of the action entities granted to the owner.
    [DataField]
    public EntProtoId CreatePortalAction = "ActionCreatePortalShadekin";

    [DataField]
    public EntProtoId CreateSecondPortalAction = "ActionCreateSecondPortalShadekin";

    [DataField]
    public EntProtoId ClosePortalsAction = "ActionClosePortalsShadekin";

    // Allow placing the two portals on different grids?
    [DataField]
    public bool AllowPortalsOnDifferentGrids;

    // Allow the portals to work across maps?
    [DataField]
    public bool AllowPortalsOnDifferentMaps;

    // Destroy the owner's own portals when they enter someone else's portal. Prevents portal chains.
    [DataField]
    public bool FizzleOnForeignPortal = true;

    // DoAfter settings for placing a portal.
    [DataField]
    public float PortalCreationDelay = 1.0f;

    [DataField]
    public bool DoAfterBreakOnDamage = true;

    [DataField]
    public bool DoAfterBreakOnMove = true;

    [DataField]
    public float DoAfterMovementThreshold = 0.5f;

    // Sounds played when a portal is created and when the pair is cleared.
    [DataField]
    public SoundSpecifier NewPortalSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
    {
        Params = AudioParams.Default.AddVolume(-2f)
    };

    [DataField]
    public SoundSpecifier ClearPortalsSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}

[Serializable, NetSerializable]
public sealed partial class ActionTeleporterDoAfterEvent : SimpleDoAfterEvent;
