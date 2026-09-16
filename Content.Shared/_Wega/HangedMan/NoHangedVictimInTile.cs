using Content.Shared.Construction;
using Content.Shared.Construction.Conditions;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.HangedMan;

/// <summary>
/// Construction condition that forbids building where a hanged victim already is.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class NoHangedVictimInTile : IConstructionCondition
{
    public const string GuidebookString = "construction-step-condition-no-hanged-victim";

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var xformSys = entMan.System<SharedTransformSystem>();
        var mapSys = entMan.System<SharedMapSystem>();

        var gridUid = xformSys.GetGrid(location);
        if (!entMan.TryGetComponent<MapGridComponent>(gridUid, out var grid))
            return true;

        var tile = mapSys.LocalToTile(gridUid.Value, grid, location);
        var enumerator = mapSys.GetAnchoredEntitiesEnumerator(gridUid.Value, grid, tile);
        while (enumerator.MoveNext(out var ent))
        {
            if (entMan.HasComponent<HangedManVictimComponent>(ent.Value))
                return false;
        }

        return true;
    }

    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = GuidebookString
        };
    }
}
