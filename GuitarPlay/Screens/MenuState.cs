using GuitarPlay.Music;

namespace GuitarPlay.Screens;

public enum Activity { Constellations, Hunt, NameIt, Track }

/// <summary>O que está escolhido no menu; também serve para "tocar de novo" e voltar ao mesmo lugar.</summary>
public sealed record MenuState(int LevelIndex = 0, Activity Activity = Activity.Constellations, int BpmIndex = 0)
{
    public static readonly int[] BpmOptions = { 60, 80, 100, 120 };

    public Level Level => LevelFactory.World1[LevelIndex];
    public int Bpm => BpmOptions[BpmIndex];

    public IScreen CreateScreen(GuitarPlayGame game) => Activity switch
    {
        Activity.Constellations => new ConstellationScreen(game, this),
        Activity.Hunt => new HuntScreen(game, this),
        Activity.NameIt => new NameItScreen(game, this),
        _ => new PlayScreen(game, this),
    };

    public static string NameOf(Activity activity) => activity switch
    {
        Activity.Constellations => "Constelações",
        Activity.Hunt => "Caça ao intervalo",
        Activity.NameIt => "Qual é essa nota?",
        _ => "Pista",
    };

    public static string[] DescriptionOf(Activity activity) => activity switch
    {
        Activity.Constellations => new[]
        {
            "Lição guiada: a partir da tônica, cada intervalo",
            "aparece como um caminho fixo no braço",
            "(quantas cordas e quantas casas), com som.",
        },
        Activity.Hunt => new[]
        {
            "60 segundos: clique em todas as notas do",
            "intervalo pedido. Só as tônicas aparecem;",
            "o resto você precisa achar de cabeça.",
        },
        Activity.NameIt => new[]
        {
            "Uma nota acende e toca junto com a tônica.",
            "Responda o grau nas teclas 1 a 4.",
            "12 perguntas, valendo sequência e rapidez.",
        },
        _ => new[]
        {
            "As notas descem pela pista no tempo do",
            "metrônomo. Teclas 1 a 6 ou A S D F G H",
            "tocam as cordas (6ª corda à esquerda).",
        },
    };
}
