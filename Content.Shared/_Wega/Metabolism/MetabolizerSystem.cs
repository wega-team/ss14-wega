namespace Content.Shared.Metabolism;

public sealed partial class MetabolizerSystem
{
    public void ClearMetabolizerTypes(MetabolizerComponent component)
    {
        if (component.MetabolizerTypes != null)
            component.MetabolizerTypes.Clear();
    }
}
