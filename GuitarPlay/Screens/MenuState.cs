using GuitarPlay.Gameplay;
using GuitarPlay.Music;

namespace GuitarPlay.Screens;

public enum Activity { Constellations, Hunt, NameIt, Track, Training }

/// <summary>O que está escolhido no menu; também serve para "tocar de novo" e voltar ao mesmo lugar.</summary>
public sealed record MenuState(
    int WorldIndex = 0,
    int LevelIndex = 0,
    Activity Activity = Activity.Constellations,
    int BpmIndex = 0,
    int Assist = 1)
{
    public static readonly int[] BpmOptions = { 60, 80, 100, 120 };
    public const int MaxAssist = Unlocks.MaxAssist;

    /// <summary>Atividades de cada nível, na ordem do menu (o Treino do dia fica à parte).</summary>
    public static readonly Activity[] PerShape = { Activity.Constellations, Activity.Hunt, Activity.NameIt, Activity.Track };

    public LevelRef At => new(WorldIndex, LevelIndex);
    public World World => LevelFactory.Worlds[WorldIndex];
    public Level Level => LevelFactory.Get(At);
    public int Bpm => BpmOptions[BpmIndex];

    /// <summary>Níveis que não são de um desenho só (recortes, transições, pares de cordas) têm só a Pista.</summary>
    public bool Supports(Activity activity) => activity == Activity.Track || activity == Activity.Training || Level.IsShape;

    public string ProgressKey => Progress.StarsKey(At, Activity == Activity.Track ? Unlocks.TrackActivity : Activity.ToString(),
        Activity == Activity.Track ? Assist : 0);

    public IScreen CreateScreen(GuitarPlayGame game) => (Supports(Activity) ? Activity : Activity.Track) switch
    {
        Activity.Constellations => new ConstellationScreen(game, this),
        Activity.Hunt => new HuntScreen(game, this),
        Activity.NameIt => new NameItScreen(game, this),
        Activity.Training => new TrainingScreen(game, this),
        _ => new PlayScreen(game, this with { Activity = Activity.Track }),
    };

    public static string AssistName(int assist) => assist switch
    {
        1 => "Cor + grau",
        2 => "Só a cor",
        3 => "Só a tônica",
        _ => "Sem ajuda",
    };

    public static string NameOf(Activity activity) => activity switch
    {
        Activity.Constellations => "Constelações",
        Activity.Hunt => "Caça ao intervalo",
        Activity.NameIt => "Qual é essa nota?",
        Activity.Training => "Treino do dia",
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
            "60 segundos: ache todas as notas do intervalo",
            "pedido. Só as tônicas aparecem; o resto",
            "você precisa achar de cabeça.",
        },
        Activity.NameIt => new[]
        {
            "Uma nota acende e toca junto com a tônica.",
            "Responda o grau nas teclas 1 a 4.",
            "12 perguntas, valendo sequência e rapidez.",
        },
        _ => new[]
        {
            "As notas descem no tempo do metrônomo.",
            "2★ aqui liberam o próximo nível e diminuem",
            "a ajuda visual (cor + grau → sem ajuda).",
        },
    };
}
