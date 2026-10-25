using System.Text.Json.Serialization;

namespace GuitarPlay.Gameplay;

/// <summary>Quanto você domina um intervalo de um desenho (sistema de caixas de Leitner).</summary>
public sealed class ReviewItem
{
    public int Box { get; set; } = 1;
    public DateTime Due { get; set; }
    public int Seen { get; set; }
    public int Correct { get; set; }

    [JsonIgnore]
    public double Accuracy => Seen == 0 ? 0 : Correct / (double)Seen;
}

/// <summary>
/// Progresso salvo em %AppData%\GuitarPlay\progress.json: melhores estrelas de cada atividade
/// e a repetição espaçada de cada intervalo (desenho + grau).
/// </summary>
public sealed class Progress
{
    public const int MaxBox = 5;
    // Dias até a próxima revisão para cada caixa (índice = caixa). Errou: volta para a caixa 1 (hoje).
    static readonly int[] BoxDays = { 0, 0, 1, 3, 7, 14 };

    public Dictionary<string, int> BestStars { get; set; } = new();
    public Dictionary<string, ReviewItem> Reviews { get; set; } = new();

    const string FileName = "progress.json";

    public static Progress Load() => JsonStore.Load<Progress>(FileName);

    public void Save() => JsonStore.Save(FileName, this);

    public static string StarsKey(int levelIndex, string activity, int assist = 0) => $"{levelIndex}:{activity}:{assist}";

    public int Stars(string key) => BestStars.GetValueOrDefault(key);

    public void RecordStars(string key, int stars)
    {
        if (stars <= Stars(key)) return;
        BestStars[key] = stars;
        Save();
    }

    static string ReviewKey(int levelIndex, string degree) => $"{levelIndex}:{degree}";

    public ReviewItem? Review(int levelIndex, string degree) => Reviews.GetValueOrDefault(ReviewKey(levelIndex, degree));

    public void RecordAnswer(int levelIndex, string degree, bool correct)
    {
        string key = ReviewKey(levelIndex, degree);
        var item = Reviews.GetValueOrDefault(key) ?? new ReviewItem();
        item.Seen++;
        if (correct)
        {
            item.Correct++;
            item.Box = Math.Min(MaxBox, item.Box + 1);
        }
        else item.Box = 1;
        item.Due = DateTime.Today.AddDays(BoxDays[item.Box]);
        Reviews[key] = item;
        Save();
    }

    public bool IsDue(int levelIndex, string degree) => Review(levelIndex, degree) is { } r && r.Due <= DateTime.Today;
}
