using GuitarPlay.Music;

namespace GuitarPlay.Gameplay;

/// <summary>Um item de revisão: um intervalo dentro de um desenho.</summary>
public readonly record struct TrainingItem(int LevelIndex, string Degree);

/// <summary>Monta o Treino do dia: revisões vencidas primeiro, depois poucos itens novos, depois os mais errados.</summary>
public static class TrainingPlan
{
    public const int Questions = 12;
    const int MaxNewPerSession = 6;

    public static IEnumerable<TrainingItem> AllItems() =>
        LevelFactory.World1.SelectMany((level, index) =>
            Pentatonic.Degrees(level.Quality).Where(d => d != "1").Select(d => new TrainingItem(index, d)));

    public static int DueCount(Progress progress) => AllItems().Count(i => progress.IsDue(i.LevelIndex, i.Degree));
    public static int NewCount(Progress progress) => AllItems().Count(i => progress.Review(i.LevelIndex, i.Degree) is null);
    public static int MasteredCount(Progress progress) =>
        AllItems().Count(i => progress.Review(i.LevelIndex, i.Degree) is { Box: Progress.MaxBox });

    public static List<TrainingItem> Build(Progress progress, Random random)
    {
        var all = AllItems().ToList();
        ReviewItem? ReviewOf(TrainingItem i) => progress.Review(i.LevelIndex, i.Degree);

        var due = all.Where(i => progress.IsDue(i.LevelIndex, i.Degree))
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
