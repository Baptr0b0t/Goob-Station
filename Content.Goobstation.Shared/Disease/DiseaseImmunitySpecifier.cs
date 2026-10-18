using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Disease;

/// <summary>
/// An immunity against one genotype or multiple related genotypes.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class DiseaseImmunitySpecifier
{
    [DataField]
    public int Genotype;

    /// <summary>
    /// Hamming distance from <see cref="Genotype"/> this immunity still covers.
    /// Zero covers only the exact genotype.
    /// </summary>
    [DataField]
    public int Tolerance;

    public DiseaseImmunitySpecifier(int genotype, int tolerance)
    {
        Genotype = genotype;
        Tolerance = tolerance;
    }

    public bool Covers(int genotype)
    {
        return DiseaseGenotype.Distance(Genotype, genotype) <= Tolerance;
    }
}
