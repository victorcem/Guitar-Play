namespace GuitarPlay.Gameplay;

public static class StarRating
{
    /// <summary>Estrelas necessárias para passar de nível.</summary>
    public const int ToPass = 2;

    public static int For(double accuracy) => accuracy >= 0.95 ? 3 : accuracy >= 0.80 ? 2 : accuracy >= 0.60 ? 1 : 0;
}

/// <summary>O que a tela de resultado mostra ao fim de qualquer atividade.</summary>
/// <param name="ProgressKey">Onde guardar as melhores estrelas (null = não guarda).</param>
/// <param name="UnlockMessage">Mostrado quando o resultado libera algo novo.</param>
public sealed record ActivityResult(
    string Title,
    string Subtitle,
    double Accuracy,
    IReadOnlyList<(string Label, string Value)> Stats,
    string? ProgressKey = null,
    string? UnlockMessage = null)
{
    public int Stars => StarRating.For(Accuracy);
}
