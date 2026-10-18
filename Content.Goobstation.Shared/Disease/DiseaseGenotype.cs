using System.Numerics;
using Robust.Shared.Random;

namespace Content.Goobstation.Shared.Disease;

/// <summary>
/// Compares disease genotypes, bitfields whose differing bit count tells how
/// many mutations separate two lineages.
/// </summary>
public static class DiseaseGenotype
{
    /// <summary>
    /// Bit positions a genotype may occupy.
    /// </summary>
    public const int Bits = 31;

    /// <summary>
    /// Number of differing bits between two genotypes.
    /// </summary>
    public static int Distance(int a, int b)
    {
        return BitOperations.PopCount((uint) (a ^ b));
    }

    public static int Drift(int genotype, int bits, IRobustRandom random)
    {
        var mask = 0;
        for (var i = Bits - Math.Clamp(bits, 0, Bits); i < Bits; i++)
        {
            var bit = 1 << random.Next(i + 1);
            mask |= (mask & bit) == 0 ? bit : 1 << i;
        }

        return genotype ^ mask;
    }
}
