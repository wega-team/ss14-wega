using Content.Server.Speech.Components;
using Content.Shared.Clothing;
using Content.Shared.Toggleable; // Corvax-Wega-Add

namespace Content.Server.Speech.EntitySystems;

public sealed class AddAccentClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddAccentClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<AddAccentClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
		SubscribeLocalEvent<AddAccentClothingComponent, ToggleActionEvent>(OnToggleEvent); // Corvax-Wega-Add
    }


//  TODO: Turn this into a relay event.
    private void OnGotEquipped(EntityUid uid, AddAccentClothingComponent component, ref ClothingGotEquippedEvent args)
    {
        // does the user already has this accent?
        var componentType = Factory.GetRegistration(component.Accent).Type;
        if (HasComp(args.Wearer, componentType))
            return;

        // add accent to the user
        var accentComponent = (Component) Factory.GetComponent(componentType);
        AddComp(args.Wearer, accentComponent);

        // snowflake case for replacement accent
        if (accentComponent is ReplacementAccentComponent rep)
            rep.Accent = component.ReplacementPrototype!;

        component.IsActive = true;
    }

    private void OnGotUnequipped(EntityUid uid, AddAccentClothingComponent component, ref ClothingGotUnequippedEvent args)
    {
        if (!component.IsActive)
            return;

        // try to remove accent
        var componentType = Factory.GetRegistration(component.Accent).Type;
        RemComp(args.Wearer, componentType);

        component.IsActive = false;
    }
	
	// Corvax-Wega-Add-start
	private void OnToggleEvent(EntityUid uid, AddAccentClothingComponent component, ToggleActionEvent args)
	{
		var componentType = Factory.GetRegistration(component.Accent).Type;
		if (component.IsActive)
		{
			RemComp(args.Performer, componentType);
		    component.IsActive = false;
		}
		else
		{
			var accentComponent = (Component) Factory.GetComponent(componentType);
	        AddComp(args.Performer, accentComponent);
			component.IsActive = true;
		}
		args.Handled = true;
	}
	// Corvax-Wega-Add-end
}
