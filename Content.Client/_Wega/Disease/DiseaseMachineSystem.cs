using Robust.Client.GameObjects;
using Content.Shared.Disease;

namespace Content.Client.Disease
{
    /// <summary>
    /// Controls client-side visuals for the
    /// disease machines.
    /// </summary>
    public sealed class DiseaseMachineSystem : VisualizerSystem<DiseaseMachineVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, DiseaseMachineVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (AppearanceSystem.TryGetData<bool>(uid, DiseaseMachineVisuals.IsOn, out var isOn, args.Component)
                && AppearanceSystem.TryGetData<bool>(uid, DiseaseMachineVisuals.IsRunning, out var isRunning, args.Component))
            {
                var state = isRunning ? component.RunningState : component.IdleState;
                SpriteSystem.LayerSetVisible(uid, DiseaseMachineVisualLayers.IsOn, isOn);
                SpriteSystem.LayerSetRsiState(uid, DiseaseMachineVisualLayers.IsRunning, state);
            }
        }
    }
}

public enum DiseaseMachineVisualLayers : byte
{
    IsOn,
    IsRunning
}
