using GuitarPlay.Music;

namespace GuitarPlay.Gameplay;

/// <summary>
/// Regras de avanço: dentro de um mundo, cada nível abre com 2★ na Pista do nível anterior;
/// um mundo abre quando o último nível do mundo anterior foi passado.
/// </summary>
public static class Unlocks
{
    public const string TrackActivity = "Track";
    public const int MaxAssist = 4;

    /// <summary>Melhores estrelas na Pista do nível, em qualquer nível de ajuda.</summary>
    public static int TrackStars(Progress progress, LevelRef at) =>
        Enumerable.Range(1, MaxAssist).Max(assist => progress.Stars(Progress.StarsKey(at, TrackActivity, assist)));

    public static bool IsPassed(Progress progress, LevelRef at) => TrackStars(progress, at) >= StarRating.ToPass;

    public static bool IsWorldUnlocked(Progress progress, int world)
    {
        if (world == 0) return true;
        int last = LevelFactory.Worlds[world - 1].Levels.Count - 1;
        return IsPassed(progress, new LevelRef(world - 1, last));
    }

    public static bool IsLevelUnlocked(Progress progress, LevelRef at) =>
        IsWorldUnlocked(progress, at.World) && (at.Level == 0 || IsPassed(progress, at with { Level = at.Level - 1 }));

    /// <summary>Ajuda 1 sempre aberta; cada nível de ajuda seguinte abre com 2★ no anterior (mesmo nível).</summary>
    public static bool IsAssistUnlocked(Progress progress, LevelRef at, int assist) =>
        assist <= 1 || progress.Stars(Progress.StarsKey(at, TrackActivity, assist - 1)) >= StarRating.ToPass;

    /// <summary>O que um resultado de 2★ na Pista acabou de liberar (para mostrar na tela de resultado).</summary>
    public static string? NewlyUnlocked(Progress progress, LevelRef at, int assist)
    {
        var messages = new List<string>();
        if (assist < MaxAssist && !IsAssistUnlocked(progress, at, assist + 1))
            messages.Add($"Ajuda {assist + 1} liberada");
        if (!IsPassed(progress, at))
        {
            var world = LevelFactory.Worlds[at.World];
            if (at.Level + 1 < world.Levels.Count)
                messages.Add($"{world.Levels[at.Level + 1].Title} liberado");
            else if (at.World + 1 < LevelFactory.Worlds.Count)
                messages.Add($"Mundo {at.World + 2} liberado: {LevelFactory.Worlds[at.World + 1].Title}");
        }
        return messages.Count == 0 ? null : string.Join("  ·  ", messages);
    }
}
