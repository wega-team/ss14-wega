using Robust.Client.GameObjects;
using Content.Shared.Mining.Components;

namespace Content.Client.Mining.Visualizers
{
    public sealed class MiningServerVisualizerSystem : VisualizerSystem<MiningServerVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, MiningServerVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var miningStage = 1;
            var isActive = false;
            if (args.AppearanceData.TryGetValue(MiningServerVisuals.MiningStage, out var stageObject))
                miningStage = (int)stageObject;

            if (args.AppearanceData.TryGetValue(MiningServerVisuals.IsActive, out var activeObject))
                isActive = (bool)activeObject;

            string state;
            if (!isActive)
            {
                state = "base";
            }
            else
            {
                state = miningStage switch
                {
                    1 => "mode1",
                    2 => "mode2",
                    3 => "mode3",
                    _ => "base"
                };
            }

            SpriteSystem.LayerSetRsiState(uid, MiningServerVisualLayers.Main, state);
        }
    }

    public enum MiningServerVisualLayers : byte
    {
        Main
    }
}
