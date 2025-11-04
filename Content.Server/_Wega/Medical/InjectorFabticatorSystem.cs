using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Injector.Fabticator;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Injector.Fabticator;

public sealed class InjectorFabticatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

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
        _solutionSystem.EnsureSolution(uid, InjectorFabticatorComponent.BufferSolutionName, out _, component.BufferMaxVolume);
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

        if (!_solutionSystem.TryGetSolution(beaker, "beaker", out var beakerSolution, out var solution) ||
            !_solutionSystem.TryGetSolution(uid, InjectorFabticatorComponent.BufferSolutionName, out var bufferSolution, out _))
            return;

        if (solution.GetReagentQuantity(args.ReagentId) < args.Amount)
            return;

        var quantity = new ReagentQuantity(args.ReagentId, args.Amount);
        _solutionSystem.RemoveReagent(beakerSolution.Value, quantity);
        _solutionSystem.TryAddReagent(bufferSolution.Value, quantity, out _);

        UpdateUiState(uid, component);
    }

    private void OnTransferBufferToBeakerMessage(EntityUid uid, InjectorFabticatorComponent component, InjectorFabticatorTransferBufferToBeakerMessage args)
    {
        if (component.IsProducing)
            return;

        if (component.BeakerSlot.Item is not { } beaker)
            return;

        if (!_solutionSystem.TryGetSolution(beaker, "beaker", out var beakerSolution, out _) ||
            !_solutionSystem.TryGetSolution(uid, InjectorFabticatorComponent.BufferSolutionName, out var bufferSolution, out var solution))
            return;

        if (solution.GetReagentQuantity(args.ReagentId) < args.Amount)
            return;

        var quantity = new ReagentQuantity(args.ReagentId, args.Amount);
        _solutionSystem.RemoveReagent(bufferSolution.Value, quantity);
        _solutionSystem.TryAddReagent(beakerSolution.Value, quantity, out _);

        UpdateUiState(uid, component);
    }

    private void OnSetReagentMessage(EntityUid uid, InjectorFabticatorComponent component, InjectorFabticatorSetReagentMessage args)
    {
        if (component.IsProducing)
            return;

        if (component.Recipe == null)
            component.Recipe = new Dictionary<ReagentId, FixedPoint2>();

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

        if (component.Recipe == null || component.Recipe.Sum(r => (long)r.Value) > 30)
            return;

        var totalRequired = new Dictionary<ReagentId, FixedPoint2>();
        foreach (var (reagent, amountPerInjector) in component.Recipe)
        {
            totalRequired[reagent] = amountPerInjector * args.Amount;
        }

        if (!_solutionSystem.TryGetSolution(uid, InjectorFabticatorComponent.BufferSolutionName, out var bufferSolution, out var buffer))
            return;

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
        if (component.Recipe == null)
            return;

        var injector = Spawn(component.Injector, Transform(uid).Coordinates);
        if (!HasComp<SolutionContainerManagerComponent>(injector))
            return;

        if (!_solutionSystem.TryGetSolution(injector, "pen", out var solution, out _))
            return;

        foreach (var (reagent, amount) in component.Recipe)
        {
            var addQuantity = new ReagentQuantity(reagent, amount);
            _solutionSystem.TryAddReagent(solution.Value, addQuantity, out _);
        }

        if (!string.IsNullOrWhiteSpace(component.CustomName))
            _metaData.SetEntityName(injector, component.CustomName);

        if (_solutionSystem.TryGetSolution(uid, InjectorFabticatorComponent.BufferSolutionName, out var bufferSolution, out _))
        {
            foreach (var (reagent, amount) in component.Recipe)
            {
                var remQuantity = new ReagentQuantity(reagent, amount);
                _solutionSystem.RemoveReagent(bufferSolution.Value, remQuantity);
            }
        }
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

        _solutionSystem.TryGetSolution(uid, InjectorFabticatorComponent.BufferSolutionName, out _, out var buffer);

        var canProduce = component.Recipe != null && component.Recipe.Sum(r => (long)r.Value) <= 30;
        return new InjectorFabticatorBoundUserInterfaceState(
            component.IsProducing,
            canProduce,
            beakerNetEntity,
            beakerContainerInfo,
            buffer,
            buffer?.Volume ?? FixedPoint2.Zero,
            component.BufferMaxVolume,
            component.Recipe,
            component.CustomName,
            component.InjectorsToProduce,
            component.InjectorsProduced
        );
    }

    private ContainerInfo? BuildBeakerContainerInfo(EntityUid beaker)
    {
        if (!HasComp<SolutionContainerManagerComponent>(beaker)
            || !_solutionSystem.TryGetSolution(beaker, "beaker", out _, out var solution))
            return null;

        return new ContainerInfo(
            Name(beaker),
            solution.Volume,
            solution.MaxVolume)
        {
            Reagents = solution.Contents.ToList()
        };
    }
}
