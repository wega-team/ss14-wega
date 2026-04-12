using Content.Shared.EntityEffects.Effects.Body;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Wega.Metabolism;

[RegisterComponent]
public sealed partial class DisableMetabolismEffectsComponent : Component
{
    [DataField("allowed")]
    public List<Type> AllowedEffects = new([typeof(Oxygenate)]);
}
