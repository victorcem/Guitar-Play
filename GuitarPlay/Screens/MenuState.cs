using GuitarPlay.Gameplay;
using GuitarPlay.Music;

namespace GuitarPlay.Screens;

public enum Activity { Constellations, Hunt, NameIt, Track, Training }

/// <summary>O que está escolhido no menu; também serve para "tocar de novo" e voltar ao mesmo lugar.</summary>
public sealed record MenuState(int LevelIndex = 0, Activity Activity = Activity.Constellations, int BpmIndex = 0, int Assist = 1)
{
    public static readonly int[] BpmOptions = { 60, 80, 100, 120 };
    public const int MaxAssist = 4;

    /// <summary>Atividades de cada desenho, na ordem do menu (o Treino do dia fica à parte).</summary>
    public static readonly Activity[] PerShape = { Activity.Constellations, Activity.Hunt, Activity.NameIt, Activity.Track };

    public Level Level => LevelFactory.World1[LevelIndex];
    public int Bpm => BpmOptions[BpmIndex];

    public string ProgressKey => Progress.StarsKey(LevelIndex, Activity.ToString(), Activity == Activity.Track ? Assist : 0);

    public IScreen CreateScreen(GuitarPlayGame game) => Activity switch
    {
        Activity.Constellations => new ConstellationScreen(game, this),
        Activity.Hunt => new HuntScreen(game, this),
        Activity.NameIt => new NameItScreen(game, this),
        Activity.Training => new TrainingScreen(game, this),
        _ => new PlayScreen(game, this),
    };

    /// <summary>Ajuda 1 sempre aberta; cada nível seguinte abre com 2★ no anterior (mesmo desenho).</summary>
    public static bool IsAssistUnlocked(Progress progress, int levelIndex, int assist) =>
        assist <= 1 || progress.Stars(Progress.StarsKey(levelIndex, nameof(Activity.Track), assist - 1)) >= StarRating.ToPass;

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
            "As notas descem no tempo do metrônomo.",
            "A cada 2★ a ajuda visual diminui: cor + grau,",
            "só a cor, só a tônica e, por fim, sem ajuda.",
        },
    };
}
