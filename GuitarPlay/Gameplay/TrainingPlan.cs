using GuitarPlay.Music;

namespace GuitarPlay.Gameplay;

/// <summary>Um item de revisão: um intervalo dentro de um desenho.</summary>
public readonly record struct TrainingItem(LevelRef At, string Degree);

/// <summary>Monta o Treino do dia: revisões vencidas primeiro, depois poucos itens novos, depois os mais errados.</summary>
public static class TrainingPlan
{
    public const int Questions = 12;
    const int MaxNewPerSession = 6;

    /// <summary>Intervalos dos desenhos já liberados (menor e maior).</summary>
    public static IEnumerable<TrainingItem> AllItems(Progress progress) =>
        LevelFactory.Worlds.SelectMany((world, w) => world.Levels.Select((level, l) => (level, at: new LevelRef(w, l))))
            .Where(x => x.level.IsShape && Unlocks.IsLevelUnlocked(progress, x.at))
            .SelectMany(x => Pentatonic.Degrees(x.level.Quality).Where(d => d != "1").Select(d => new TrainingItem(x.at, d)));

    public static int DueCount(Progress progress) => AllItems(progress).Count(i => progress.IsDue(i.At, i.Degree));
    public static int NewCount(Progress progress) => AllItems(progress).Count(i => progress.Review(i.At, i.Degree) is null);
    public static int MasteredCount(Progress progress) =>
        AllItems(progress).Count(i => progress.Review(i.At, i.Degree) is { Box: Progress.MaxBox });

    public static List<TrainingItem> Build(Progress progress, Random random)
    {
        var all = AllItems(progress).ToList();
        ReviewItem? ReviewOf(TrainingItem i) => progress.Review(i.At, i.Degree);

        var due = all.Where(i => progress.IsDue(i.At, i.Degree))
            .OrderBy(i => ReviewOf(i)!.Box).ThenBy(i => ReviewOf(i)!.Due);
        // Novos na ordem do livro: o Desenho 1 vem antes do 2, e assim por diante.
        var fresh = all.Where(i => ReviewOf(i) is null).Take(MaxNewPerSession);
        var weakest = all.Where(i => ReviewOf(i) is { } r && r.Due > DateTime.Today)
            .OrderBy(i => ReviewOf(i)!.Accuracy).ThenBy(i => ReviewOf(i)!.Box);

        var picked = due.Concat(fresh).Concat(weakest).Distinct().Take(Questions).ToList();
        int distinct = picked.Count;
        for (int k = 0; picked.Count < Questions; k++) picked.Add(picked[k % distinct]);
        return picked.OrderBy(_ => random.Next()).ToList();
    }
}
