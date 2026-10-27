using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Armor;

/// <summary>
///     This handles logic relating to <see cref="ArmorComponent" />
/// </summary>
public abstract partial class SharedArmorSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<CoefficientQueryEvent>>(OnCoefficientQuery);
        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
        SubscribeLocalEvent<ArmorComponent, BorgModuleRelayedEvent<DamageModifyEvent>>(OnBorgDamageModify);
        SubscribeLocalEvent<ArmorComponent, GetVerbsEvent<ExamineVerb>>(OnArmorVerbExamine);
    }

    /// <summary>
    /// Get the total Damage reduction value of all equipment caught by the relay.
    /// </summary>
    /// <param name="ent">The item that's being relayed to</param>
    /// <param name="args">The event, contains the running count of armor percentage as a coefficient</param>
    private void OnCoefficientQuery(Entity<ArmorComponent> ent, ref InventoryRelayedEvent<CoefficientQueryEvent> args)
    {
        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            return;

        foreach (var armorCoefficient in ent.Comp.Modifiers.Coefficients)
        {
            args.Args.DamageModifiers.Coefficients[armorCoefficient.Key] = args.Args.DamageModifiers.Coefficients.TryGetValue(armorCoefficient.Key, out var coefficient) ? coefficient * armorCoefficient.Value : armorCoefficient.Value;
        }
    }

    // Corvax-Wega-ArmorPenetration-Start
    /// <summary>
    ///     Рассчитывает модификаторы защиты брони с учётом пробития.
    ///     Ослабляет положительную защиту, не изменяя уязвимости к урону.
    ///     Исходные модификаторы брони остаются неизменными.
    /// </summary>
    /// <param name="original">Исходные модификаторы брони.</param>
    /// <param name="penetration">Доля игнорируемой защиты от 0 до 1.</param>
    /// <returns>Новый набор модификаторов с учётом пробития.</returns>
    private static DamageModifierSet GetPenetratedModifiers(DamageModifierSet original, float penetration)
    {
        var modifiers = new DamageModifierSet();

        foreach (var (type, coefficient) in original.Coefficients)
        {
            modifiers.Coefficients[type] = coefficient < 1f
                ? 1f - (1f - coefficient) * (1f - penetration)
                : coefficient;
        }

        foreach (var (type, reduction) in original.FlatReductions)
        {
            modifiers.FlatReductions[type] = reduction > 0f
                ? reduction * (1f - penetration)
                : reduction;
        }

        return modifiers;
    }
    // Corvax-Wega-ArmorPenetration-End

    private void OnDamageModify(EntityUid uid, ArmorComponent component, InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (TryComp<MaskComponent>(uid, out var mask) && mask.IsToggled)
            return;

        // Corvax-Wega-ArmorPenetration-Start
        // Учитываем пробитие при расчёте защиты экипированной брони.
        var penetration = args.Args.ArmorPenetration;

        var modifiers = penetration <= 0f
            ? component.Modifiers
            : GetPenetratedModifiers(component.Modifiers, penetration);

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, modifiers);
        // Corvax-Wega-ArmorPenetration-End
    }

    private void OnBorgDamageModify(EntityUid uid, ArmorComponent component,
        ref BorgModuleRelayedEvent<DamageModifyEvent> args)
    {
        if (TryComp<MaskComponent>(uid, out var mask) && mask.IsToggled)
            return;

        // Corvax-Wega-ArmorPenetration-Start
        // Применяем пробитие к защите боргов.
        var penetration = args.Args.ArmorPenetration;

        var modifiers = penetration <= 0f
            ? component.Modifiers
            : GetPenetratedModifiers(component.Modifiers, penetration);

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, modifiers);
        // Corvax-Wega-ArmorPenetration-End
    }

    private void OnArmorVerbExamine(EntityUid uid, ArmorComponent component, GetVerbsEvent<ExamineVerb> args)
    {
		// Corvax-Wega-start
		if (component?.Modifiers == null)
			return;
		// Corvax-Wega-End

        if (!args.CanInteract || !args.CanAccess || !component.ShowArmorOnExamine)
            return;

        var examineMarkup = GetArmorExamine(component.Modifiers);

        var ev = new ArmorExamineEvent(examineMarkup);
        RaiseLocalEvent(uid, ref ev);

        _examine.AddDetailedExamineVerb(args, component, examineMarkup,
            Loc.GetString("armor-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("armor-examinable-verb-message"));
    }

    private FormattedMessage GetArmorExamine(DamageModifierSet armorModifiers)
    {
        var msg = new FormattedMessage();

	    // Corvax-Wega-Start
		if (armorModifiers == null)
			return msg;
	    // Corvax-Wega-End

        msg.AddMarkupOrThrow(Loc.GetString("armor-examine"));

        foreach (var coefficientArmor in armorModifiers.Coefficients)
        {
            msg.PushNewline();

            // TODO: probably make these prototype fields or have a test that they all exist
            var armorType = Loc.GetString("armor-damage-type-" + coefficientArmor.Key.Id.ToLower());
            msg.AddMarkupOrThrow(Loc.GetString("armor-coefficient-value",
                ("type", armorType),
                ("value", MathF.Round((1f - coefficientArmor.Value) * 100, 1))
            ));
        }

        foreach (var flatArmor in armorModifiers.FlatReductions)
        {
            msg.PushNewline();

            var armorType = Loc.GetString("armor-damage-type-" + flatArmor.Key.Id.ToLower());
            msg.AddMarkupOrThrow(Loc.GetString("armor-reduction-value",
                ("type", armorType),
                ("value", flatArmor.Value)
            ));
        }

        return msg;
    }
}
