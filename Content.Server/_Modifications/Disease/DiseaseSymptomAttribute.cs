// Developed by Nox project.
// Author: KloopRe

namespace Content.Server._Modifications.Disease;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DiseaseSymptomAttribute : Attribute
{
    public string Id { get; }

    public DiseaseSymptomAttribute(string id)
    {
        Id = id;
    }
}