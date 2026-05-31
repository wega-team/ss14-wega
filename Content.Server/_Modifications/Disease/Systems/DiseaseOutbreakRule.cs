// Developed by Nox project.
// Author: KloopRe

using System.Linq;
using Content.Server._Modifications.Disease.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._Modifications.Disease.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Modifications.Disease.Systems;

public sealed partial class DiseaseOutbreakRule : StationEventSystem<DiseaseOutbreakRuleComponent>
{
    [Dependency] private ISharedPlayerManager _playerManager = default!;
    [Dependency] private DiseaseSystem _diseaseSystem = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("DiseaseOutbreakRule");
    }

    protected override void Added(EntityUid uid, DiseaseOutbreakRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        List<EntityUid> ents = new List<EntityUid>();

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is { } entity)
                ents.Add(entity);
        }

        if (component.Data == null)
            component.Data = _diseaseSystem.GenerateDiseaseData(component.SymptomsByDanger, component.BodyCount);

        var validEntities = ents
            .Where(ent => _diseaseSystem.CanInfect(ent, component.Data))
            .ToList();

        if (validEntities.Count <= 0)
        {
            _sawmill.Info("Не найдено сущностей, которых можно подвергнуть заражению вирусом.");
            return;
        }

        var toAdd = Math.Min(component.NumberPrimaryPatients, validEntities.Count);

        for (var i = 0; i < toAdd; i++)
        {
            var picked = _random.PickAndTake(validEntities);

            _diseaseSystem.InfectEntity(component.Data, picked);

            var comp = EnsureComp<PrimaryPatientComponent>(picked);
            comp.StrainId = component.Data.StrainId;

            if (validEntities.Count == 0)
                break;
        }

    }
}
