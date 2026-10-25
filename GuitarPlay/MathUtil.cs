namespace GuitarPlay;

public static class MathUtil
{
    /// <summary>Resto sempre positivo (o % do C# devolve negativo para números negativos).</summary>
    public static int Mod(int value, int modulus) => ((value % modulus) + modulus) % modulus;

    public static double Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }
}
