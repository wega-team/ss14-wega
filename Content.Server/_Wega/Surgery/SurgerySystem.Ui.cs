using System.Linq;
using Content.Server.Kitchen.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Surgery;
using Content.Shared.Surgery.Components;
using Robust.Shared.Timing;

namespace Content.Server.Surgery;

public sealed partial class SurgerySystem
{
    private void UiInitialize()
    {
        SubscribeLocalEvent<OperatedComponent, AfterInteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<OperatedComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<OperatedComponent, UnbuckledEvent>(OnUnbuckled);
    }

    private void OnInteractUsing(EntityUid uid, OperatedComponent comp, AfterInteractUsingEvent args)
    {
        if (!HasComp<SharpComponent>(args.Used))
            return;

        if (!TryGetOperatingTable(uid, out _) && !comp.OperatedPart)
            return;

        OpenSurgeryUi(args.User, uid);
    }

    private void OpenSurgeryUi(EntityUid user, EntityUid patient)
    {
        if (_ui.IsUiOpen(patient, SurgeryUiKey.Key))
            return;

        _ui.OpenUi(patient, SurgeryUiKey.Key, user);
    }

    private void OnUiOpened(EntityUid uid, OperatedComponent comp, BoundUIOpenedEvent args)
    {
        if (comp.GraphId == null)
            return;

        var graph = _proto.Index<SurgeryGraphPrototype>(comp.GraphId);
        Timer.Spawn(250, () =>
        {
            UpdateUi(uid, comp, graph);
        });
    }

    private void OnUnbuckled(Entity<OperatedComponent> ent, ref UnbuckledEvent args)
    {
        if (!_ui.IsUiOpen(ent.Owner, SurgeryUiKey.Key))
            return;

        _ui.CloseUi(ent.Owner, SurgeryUiKey.Key);
    }

    private void UpdateUi(EntityUid patient, OperatedComponent comp, SurgeryGraphPrototype graph)
    {
        if (!_ui.HasUi(patient, SurgeryUiKey.Key))
            return;

        var groups = new List<SurgeryGroupDto>();
        if (comp.CurrentNode == "Default")
        {
            var startNodes = graph.GetStartNodes().ToList();
            foreach (var startNodeProto in startNodes)
            {
                var startNode = ConvertToRuntimeNode(startNodeProto);
                AddNodeSteps(startNode, patient, comp, groups);
            }
        }
        else if (_proto.TryIndex(comp.CurrentNode, out SurgeryNodePrototype? nodeProto))
        {
            var node = ConvertToRuntimeNode(nodeProto);
            AddNodeSteps(node, patient, comp, groups);
        }

        _ui.ServerSendUiMessage(patient, SurgeryUiKey.Key,
            new SurgeryProcedureDto(groups, GetNetEntity(patient)));
    }
}
