using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Injector.Fabticator;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Injector.Fabticator;

public sealed partial class InjectorFabticatorSystem : EntitySystem
{
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectorFabticatorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<InjectorFabticatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InjectorFabticatorComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<InjectorFabticatorComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<InjectorFabticatorComponent, BoundUIOpenedEvent>(OnUIOpened);

        SubscribeLocalEvent<InjectorFabticatorComponent, InjectorFabticatorTransferBeakerToBufferMessage>(OnTransferBeakerToBufferMessage);
        SubscribeLocalEvent<InjectorFabticatorComponent, InjectorFabticatorTransferBufferToBeakerMessage>(OnTransferBufferToBeakerMessage);
        SubscribeLocalEvent<InjectorFabticatorComponent, InjectorFabticatorSetReagentMessage>(OnSetReagentMessage);
        SubscribeLocalEvent<InjectorFabticatorComponent, InjectorFabticatorRemoveReagentMessage>(OnRemoveReagentMessage);
        SubscribeLocalEvent<InjectorFabticatorComponent, InjectorFabticatorProduceMessage>(OnProduceMessage);
        SubscribeLocalEvent<InjectorFabticatorComponent, InjectorFabticatorEjectMessage>(OnEjectMessage);
        SubscribeLocalEvent<InjectorFabticatorComponent, InjectorFabticatorSyncRecipeMessage>(OnSyncRecipeMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InjectorFabticatorComponent>();
        while (query.MoveNext(out var uid, out var injectorFabticator))
        {
            if (!injectorFabticator.IsProducing || !this.IsPowered(uid, EntityManager))
                return;

            injectorFabticator.ProductionTimer += frameTime;
            if (injectorFabticator.ProductionTimer >= injectorFabticator.ProductionTime)
            {
                injectorFabticator.ProductionTimer = 0f;
                ProduceInjector(uid, injectorFabticator);
                injectorFabticator.InjectorsProduced++;

                if (injectorFabticator.InjectorsProduced >= injectorFabticator.InjectorsToProduce)
                {
                    injectorFabticator.IsProducing = false;
                    injectorFabticator.InjectorsToProduce = 0;
                    injectorFabticator.InjectorsProduced = 0;
                    injectorFabticator.Recipe = null;

                    _ambient.SetAmbience(uid, false);
                }

                UpdateAppearance(uid, injectorFabticator);
                UpdateUiState(uid, injectorFabticator);
            }
        }
    }

    private void OnComponentInit(EntityUid uid, InjectorFabticatorComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, InjectorFabticatorComponent.BeakerSlotId, component.BeakerSlot);
    }

    private void OnMapInit(EntityUid uid, InjectorFabticatorComponent component, MapInitEvent args)
    {
        if (!TryComp<SolutionComponent>(uid, out var solutionComp))
        {
            solutionComp = AddComp<SolutionComponent>(uid);
            solutionComp.Solution.MaxVolume = component.BufferMaxVolume;
        }
        else
        {
            solutionComp.Solution.MaxVolume = component.BufferMaxVolume;
        }

        solutionComp.Solution.RemoveAllSolution();
    }

    private void OnContainerModified(EntityUid uid, InjectorFabticatorComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID == InjectorFabticatorComponent.BeakerSlotId)
            UpdateUiState(uid, component);
    }

    private void OnUIOpened(EntityUid uid, InjectorFabticatorComponent component, BoundUIOpenedEvent args)
    {
        UpdateUiState(uid, component);
    }

    private void OnTransferBeakerToBufferMessage(EntityUid uid, InjectorFabticatorComponent component, InjectorFabticatorTransferBeakerToBufferMessage args)
    {
        if (component.IsProducing || component.BeakerSlot.Item is not { } beaker)
            return;

        if (!TryComp<SolutionComponent>(uid, out var bufferSolutionComp))
            return;

        if (!TryComp<SolutionComponent>(beaker, out var beakerSolutionComp))
            return;

        var bufferSolution = bufferSolutionComp.Solution;
        var beakerSolution = beakerSolutionComp.Solution;

        if (!beakerSolution.TryGetReagentQuantity(args.ReagentId, out var availableAmount))
            return;

        if (availableAmount < args.Amount)
            return;

        if (bufferSolution.AvailableVolume < args.Amount)
            return;

        var reagentToTransfer = new ReagentQuantity(args.ReagentId, args.Amount);

        var removed = beakerSolution.RemoveReagent(reagentToTransfer);
        if (removed <= FixedPoint2.Zero)
            return;

        bufferSolution.AddReagent(args.ReagentId, removed);

        _solutionSystem.UpdateChemicals((beaker, beakerSolutionComp));
        _solutionSystem.UpdateChemicals((uid, bufferSolutionComp));

        UpdateUiState(uid, component);
    }

    private void OnTransferBufferToBeakerMessage(EntityUid uid, InjectorFabticatorComponent component, InjectorFabticatorTransferBufferToBeakerMessage args)
    {
        if (component.IsProducing)
            return;

        if (component.BeakerSlot.Item is not { } beaker)
            return;

        if (!TryComp<SolutionComponent>(uid, out var bufferSolutionComp))
            return;

        if (!TryComp<SolutionComponent>(beaker, out var beakerSolutionComp))
            return;

        var bufferSolution = bufferSolutionComp.Solution;
        var beakerSolution = beakerSolutionComp.Solution;

        if (!bufferSolution.TryGetReagentQuantity(args.ReagentId, out var availableAmount))
            return;

        if (availableAmount < args.Amount)
            return;

        if (beakerSolution.AvailableVolume < args.Amount)
            return;

        var reagentToTransfer = new ReagentQuantity(args.ReagentId, args.Amount);

        var removed = bufferSolution.RemoveReagent(reagentToTransfer);
        if (removed <= FixedPoint2.Zero)
            return;

        beakerSolution.AddReagent(args.ReagentId, removed);

        _solutionSystem.UpdateChemicals((uid, bufferSolutionComp));
        _solutionSystem.UpdateChemicals((beaker, beakerSolutionComp));

        UpdateUiState(uid, component);
    }

    private void OnSetReagentMessage(EntityUid uid, InjectorFabticatorComponent component, InjectorFabticatorSetReagentMessage args)
    {
        if (component.IsProducing)
            return;

        if (component.Recipe == null)
            component.Recipe = new Dictionary<ReagentId, FixedPoint2>();

        if (!TryComp<SolutionComponent>(uid, out var bufferSolutionComp))
            return;

        var bufferSolution = bufferSolutionComp.Solution;
        if (!bufferSolution.TryGetReagentQuantity(args.ReagentId, out var availableAmount))
            return;

        if (availableAmount < args.Amount)
            return;

        var exactKey = component.Recipe.Keys.FirstOrDefault(k =>
            k.Prototype == args.ReagentId.Prototype);
        if (exactKey != default)
        {
            component.Recipe[exactKey] += args.Amount;
        }
        else
        {
            component.Recipe[args.ReagentId] = args.Amount;
        }

        UpdateUiState(uid, component);
    }

    private void OnRemoveReagentMessage(EntityUid uid, InjectorFabticatorComponent component, InjectorFabticatorRemoveReagentMessage args)
    {
        if (component.IsProducing || component.Recipe == null)
            return;

        var exactKey = component.Recipe.Keys.FirstOrDefault(k =>
            k.Prototype == args.ReagentId.Prototype);
        if (exactKey != default)
            component.Recipe.Remove(exactKey);

        UpdateUiState(uid, component);
    }

    private void OnProduceMessage(EntityUid uid, InjectorFabticatorComponent component, InjectorFabticatorProduceMessage args)
    {
        if (component.IsProducing)
            return;

        if (component.Recipe == null || component.Recipe.Count == 0)
            return;

        var totalAmount = component.Recipe.Sum(r => (long)r.Value);
        if (totalAmount > 30)
            return;

        if (!TryComp<SolutionComponent>(uid, out var bufferSolutionComp))
            return;

        var bufferSolution = bufferSolutionComp.Solution;
        foreach (var (reagentId, amountPerInjector) in component.Recipe)
        {
            var requiredAmount = amountPerInjector * args.Amount;
            if (!bufferSolution.TryGetReagentQuantity(reagentId, out var availableAmount))
                return;

            if (availableAmount < requiredAmount)
                return;
        }

        component.CustomName = args.CustomName;
        component.InjectorsToProduce = args.Amount;
        component.InjectorsProduced = 0;
        component.IsProducing = true;
        component.ProductionTimer = 0f;

        _ambient.SetAmbience(uid, true);

        UpdateAppearance(uid, component);
        UpdateUiState(uid, component);
    }

    private void OnEjectMessage(EntityUid uid, InjectorFabticatorComponent component, InjectorFabticatorEjectMessage args)
    {
        if (component.IsProducing)
            return;

        _itemSlotsSystem.TryEject(uid, component.BeakerSlot, null, out var _, true);
    }

    private void OnSyncRecipeMessage(EntityUid uid, InjectorFabticatorComponent component, InjectorFabticatorSyncRecipeMessage args)
    {
        if (component.IsProducing)
            return;

        component.Recipe = args.Recipe;
        UpdateUiState(uid, component);
    }

    private void ProduceInjector(EntityUid uid, InjectorFabticatorComponent component)
    {
        if (component.Recipe == null || component.Recipe.Count == 0)
            return;

        var injector = Spawn(component.Injector, Transform(uid).Coordinates);
        if (!TryComp<SolutionComponent>(injector, out var injectorSolutionComp))
        {
            injectorSolutionComp = AddComp<SolutionComponent>(injector);
            injectorSolutionComp.Solution.MaxVolume = 30;
        }

        var injectorSolution = injectorSolutionComp.Solution;
        if (!TryComp<SolutionComponent>(uid, out var bufferSolutionComp))
            return;

        var bufferSolution = bufferSolutionComp.Solution;
        foreach (var (reagent, amount) in component.Recipe)
        {
            if (!bufferSolution.TryGetReagentQuantity(reagent, out var availableAmount))
                continue;

            var amountToTransfer = FixedPoint2.Min(amount, availableAmount);
            if (amountToTransfer <= FixedPoint2.Zero)
                continue;

            var reagentToTransfer = new ReagentQuantity(reagent, amountToTransfer);

            var removed = bufferSolution.RemoveReagent(reagentToTransfer);
            if (removed <= FixedPoint2.Zero)
                continue;

            injectorSolution.AddReagent(reagent, removed);
        }

        _solutionSystem.UpdateChemicals((uid, bufferSolutionComp));
        _solutionSystem.UpdateChemicals((injector, injectorSolutionComp));

        if (!string.IsNullOrWhiteSpace(component.CustomName))
            _metaData.SetEntityName(injector, component.CustomName);
    }

    private void UpdateAppearance(EntityUid uid, InjectorFabticatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _appearance.SetData(uid, InjectorFabticatorVisuals.IsRunning, component.IsProducing);
    }

    private void UpdateUiState(EntityUid uid, InjectorFabticatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = GetUserInterfaceState(uid, component);
        _uiSystem.SetUiState(uid, InjectorFabticatorUiKey.Key, state);
    }

    private InjectorFabticatorBoundUserInterfaceState GetUserInterfaceState(EntityUid uid, InjectorFabticatorComponent component)
    {
        NetEntity? beakerNetEntity = null;
        ContainerInfo? beakerContainerInfo = null;

        if (component.BeakerSlot.Item != null)
        {
            beakerNetEntity = GetNetEntity(component.BeakerSlot.Item);
            beakerContainerInfo = BuildBeakerContainerInfo(component.BeakerSlot.Item.Value);
        }

        Solution? buffer = null;
        FixedPoint2 bufferVolume = FixedPoint2.Zero;

        if (TryComp<SolutionComponent>(uid, out var bufferSolutionComp))
        {
            buffer = bufferSolutionComp.Solution;
            bufferVolume = buffer.Volume;
        }

        bool canProduce = false;
        if (component.Recipe != null && component.Recipe.Count > 0 && buffer != null)
        {
            var totalAmount = component.Recipe.Sum(r => (long)r.Value);
            if (totalAmount <= 30)
            {
                canProduce = true;
                foreach (var (reagentId, amount) in component.Recipe)
                {
                    var availableAmount = buffer.GetReagentQuantity(reagentId);
                    if (availableAmount < amount)
                    {
                        canProduce = false;
                        break;
                    }
                }
            }
        }

        return new InjectorFabticatorBoundUserInterfaceState(
            component.IsProducing,
            canProduce,
            beakerNetEntity,
            beakerContainerInfo,
            buffer,
            bufferVolume,
            component.BufferMaxVolume,
            component.Recipe,
            component.CustomName,
            component.InjectorsToProduce,
            component.InjectorsProduced
        );
    }

    private ContainerInfo? BuildBeakerContainerInfo(EntityUid beaker)
    {
        if (!TryComp<SolutionComponent>(beaker, out var solutionComp))
            return null;

        var solution = solutionComp.Solution;
        return new ContainerInfo(
            Name(beaker),
            solution.Volume,
            solution.MaxVolume)
        {
            Reagents = solution.Contents.ToList()
        };
    }
}
