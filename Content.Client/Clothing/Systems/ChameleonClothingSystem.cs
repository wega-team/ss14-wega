using System.Linq;
using Content.Client.PDA;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.Systems;

// All valid items for chameleon are calculated on client startup and stored in dictionary.
public sealed class ChameleonClothingSystem : SharedChameleonClothingSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, AfterAutoHandleStateEvent>(HandleState);

        PrepareAllVariants();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReloaded);
    }

    private void OnProtoReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            PrepareAllVariants();
    }

    // Caches visualizers temporarily stripped from ninja-disguised items so their own
    // visualizers don't fight sprite.CopyFrom (e.g. ninja glove toggle states or PDA base-state).
    private readonly Dictionary<EntityUid, GenericVisualizerComponent> _suppressedVisualizers = new();
    private readonly Dictionary<EntityUid, Content.Client.PDA.PdaVisualsComponent> _suppressedPdaVisuals = new();

    private void HandleState(EntityUid uid, ChameleonClothingComponent component, ref AfterAutoHandleStateEvent args)
    {
        // Skip appearance/contraband for items where ChameleonClothingComponent was added
        // dynamically (ninja disguise system). Their own GenericVisualizers would conflict
        // with sprite.CopyFrom if AppendData ran after the sprite was replaced.
        var ownProtoId = MetaData(uid).EntityPrototype?.ID;
        var ninjaManaged = MetaData(uid).EntityPrototype?.TryGetComponent(out ChameleonClothingComponent? _, Factory) != true;

        if (ninjaManaged)
        {
            // Disguised to a foreign prototype: strip the item's own visualizers so their
            // states (which reference the ninja/holo RSI) aren't applied to the copied sprite.
            // Restored to its own prototype: put them back.
            var disguised = component.Default != null && component.Default != ownProtoId;

            if (disguised)
            {
                if (TryComp<GenericVisualizerComponent>(uid, out var vis) && !_suppressedVisualizers.ContainsKey(uid))
                {
                    _suppressedVisualizers[uid] = vis;
                    RemComp<GenericVisualizerComponent>(uid);
                }
                if (TryComp<Content.Client.PDA.PdaVisualsComponent>(uid, out var pdaVis) && !_suppressedPdaVisuals.ContainsKey(uid))
                {
                    _suppressedPdaVisuals[uid] = pdaVis;
                    RemComp<Content.Client.PDA.PdaVisualsComponent>(uid);
                }
            }
            else
            {
                if (_suppressedVisualizers.Remove(uid, out var cached))
                    AddComp(uid, cached, overwrite: true);
                if (_suppressedPdaVisuals.Remove(uid, out var cachedPda))
                    AddComp(uid, cachedPda, overwrite: true);
            }
        }

        UpdateVisuals(uid, component, skipAppearance: ninjaManaged);
    }

    protected override void UpdateSprite(EntityUid uid, EntityPrototype proto)
    {
        base.UpdateSprite(uid, proto);
        if (TryComp(uid, out SpriteComponent? sprite)
            && proto.TryGetComponent(out SpriteComponent? otherSprite, Factory))
        {
            sprite.CopyFrom(otherSprite);
        }

        // Edgecase for PDAs to include visuals when UI is open
        if (TryComp(uid, out PdaBorderColorComponent? borderColor)
            && proto.TryGetComponent(out PdaBorderColorComponent? otherBorderColor, Factory))
        {
            borderColor.BorderColor = otherBorderColor.BorderColor;
            borderColor.AccentHColor = otherBorderColor.AccentHColor;
            borderColor.AccentVColor = otherBorderColor.AccentVColor;
        }
    }
}
