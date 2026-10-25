namespace GuitarPlay;

public static class MathUtil
{
    /// <summary>Resto sempre positivo (o % do C# devolve negativo para números negativos).</summary>
    public static int Mod(int value, int modulus) => ((value % modulus) + modulus) % modulus;
}
