namespace GuitarPlay.Gameplay;

public static class StarRating
{
    /// <summary>Estrelas necessárias para passar de nível.</summary>
    public const int ToPass = 2;

    public static int For(double accuracy) => accuracy >= 0.95 ? 3 : accuracy >= 0.80 ? 2 : accuracy >= 0.60 ? 1 : 0;
}

/// <summary>O que a tela de resultado mostra ao fim de qualquer atividade.</summary>
public sealed record ActivityResult(string Title, string Subtitle, double Accuracy, IReadOnlyList<(string Label, string Value)> Stats)
{
    public int Stars => StarRating.For(Accuracy);
}
