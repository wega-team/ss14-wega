using Content.Shared._Wega.Silicons.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Silicons.Borgs
{
    public abstract partial class SharedAiRemoteControlSystem : EntitySystem
    {
        [Dependency] private SharedStationAiSystem _stationAiSystem = default!;
        [Dependency] private SharedTransformSystem _xformSystem = default!;
        [Dependency] private SharedMindSystem _mind = default!;
        [Dependency] private SharedActionsSystem _actions = default!;
        public override void Initialize()
        {
            base.Initialize();
        }

        public void ReturnMindIntoAi(EntityUid entity)
        {
            if (!TryComp<AiRemoteControllerComponent>(entity, out var remoteComp))
                return;

            if (remoteComp.AiHolder == null || Deleted(remoteComp.AiHolder.Value))
                return;

            if (!_stationAiSystem.TryGetCore(remoteComp.AiHolder.Value, out var stationAiCore) || stationAiCore.Comp?.RemoteEntity == null)
                return;

            if (remoteComp.LinkedMind == null)
                return;

            if (!TryComp<StationAiHeldComponent>(remoteComp.AiHolder.Value, out var stationAiHeldComp))
                return;

            stationAiHeldComp.CurrentConnectedEntity = null;
            _mind.TransferTo(remoteComp.LinkedMind.Value, remoteComp.AiHolder);
            _stationAiSystem.SwitchRemoteEntityMode(stationAiCore, true);

            if (_actions == null)
                return;

            if (remoteComp.BackToAiActionEntity != null)
            {
                _actions.RemoveAction(entity, remoteComp.BackToAiActionEntity);
                remoteComp.BackToAiActionEntity = null;
            }

            remoteComp.AiHolder = null;
            remoteComp.LinkedMind = null;

            _xformSystem.SetCoordinates(stationAiCore.Comp.RemoteEntity.Value, Transform(entity).Coordinates);
        }
    }

    [Serializable, NetSerializable]
    public sealed partial class ReturnMindIntoAiEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public sealed partial class ToggleRemoteDevicesScreenEvent : HandledEntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public enum RemoteDeviceUiKey : byte
    {
        Key
    }
}