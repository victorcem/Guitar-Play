namespace GuitarPlay.Music;

/// <summary>Uma nota da pista: em que tempo (em batidas) ela chega e onde tocar.</summary>
public sealed record ChartNote(double Beat, int String, int Fret, int Midi, string Degree);

/// <param name="ShapeNotes">Notas mostradas no mini braço (o desenho, ou a união dos desenhos usados).</param>
/// <param name="IsShape">Nível de um desenho só: aceita também Constelações, Caça e Qual é essa nota?.</param>
public sealed record Level(
    string Title,
    string Subtitle,
    int TonicPc,
    ScaleQuality Quality,
    IReadOnlyList<ScaleNote> ShapeNotes,
    IReadOnlyList<ChartNote> Notes,
    bool IsShape = false)
{
    public string KeyName => $"{NoteNames.Of(TonicPc)} {(Quality == ScaleQuality.Minor ? "menor" : "maior")}";
}

/// <summary>Um capítulo do livro: uma lista de níveis que se desbloqueiam em sequência.</summary>
public sealed record World(string Title, string Chapter, IReadOnlyList<Level> Levels);

/// <summary>Endereço de um nível: mundo e posição dentro dele.</summary>
public readonly record struct LevelRef(int World, int Level);

public static class LevelFactory
{
    public const int Sol = 7;

    static IReadOnlyList<World>? _worlds;

    public static IReadOnlyList<World> Worlds => _worlds ??= new[]
    {
        new World("Os 5 desenhos", "Capítulo 1", ShapeLevels(ScaleQuality.Minor)),
        new World("Pentatônica maior", "Capítulo 2", ShapeLevels(ScaleQuality.Major).Append(MinorMajorSwitch()).ToList()),
        new World("Recortes de oitava", "Capítulo 3", OctaveCuts()),
        new World("Transições entre desenhos", "Capítulo 4", Transitions()),
        new World("Pares de cordas", "Capítulo 5", StringPairs()),
    };

    public static Level Get(LevelRef at) => Worlds[at.World].Levels[at.Level];

    static string KeyName(ScaleQuality quality) => $"{NoteNames.Of(Sol)} {(quality == ScaleQuality.Minor ? "menor" : "maior")}";

    static List<ChartNote> Chart(IEnumerable<ScaleNote> path) =>
        path.Select((n, i) => new ChartNote(i, n.String, n.Fret, n.Midi, n.Degree)).ToList();

    static List<ScaleNote> Distinct(IEnumerable<ScaleNote> notes) =>
        notes.DistinctBy(n => (n.String, n.Fret)).ToList();

    static IEnumerable<ScaleNote> UpAndDown(IReadOnlyList<ScaleNote> notes) => notes.Concat(notes.Reverse().Skip(1));

    // Capítulos 1 e 2: cada desenho sobe e desce, na ordem do braço (na maior, o Desenho 1 fica na casa 12).
    static List<Level> ShapeLevels(ScaleQuality quality) =>
        Pentatonic.ShapesUpTheNeck(Sol, quality).Zip(Pentatonic.ShapeOrder(quality), (notes, shape) =>
            new Level($"Desenho {shape}", $"{KeyName(quality)} · sobe e desce", Sol, quality, notes,
                Chart(UpAndDown(notes)), IsShape: true)).ToList();

    // Capítulo 2, passo 2: a mesma região do braço trocando de sonoridade (menor → maior → menor).
    static Level MinorMajorSwitch()
    {
        var minor = Pentatonic.Shape(Sol, ScaleQuality.Minor, 1);
        var major = Pentatonic.Shape(Sol, ScaleQuality.Major, 2);
        var path = UpAndDown(minor).Concat(UpAndDown(major)).Concat(UpAndDown(minor));
        return new Level("Menor ↔ Maior", "Sol menor (Desenho 1) → Sol maior (Desenho 2) → Sol menor", Sol,
            ScaleQuality.Minor, Distinct(minor.Concat(major)), Chart(path));
    }

    // Capítulo 3: recortes de uma oitava dentro de cada desenho (tônica a tônica, terça a terça, quinta a quinta).
    static List<Level> OctaveCuts()
    {
        var levels = new List<Level>();
        foreach (var quality in new[] { ScaleQuality.Minor, ScaleQuality.Major })
        {
            string third = quality == ScaleQuality.Minor ? "b3" : "3";
            foreach (var (degree, name) in new[] { ("1", "Tônica a tônica"), (third, "Terça a terça"), ("5", "Quinta a quinta") })
            {
                var shapes = Pentatonic.ShapesUpTheNeck(Sol, quality);
                var path = shapes.SelectMany(shape => OctaveCut(shape, degree));
                levels.Add(new Level(name, $"{KeyName(quality)} · todos os desenhos", Sol, quality,
                    Distinct(shapes.SelectMany(s => s)), Chart(path)));
            }
        }
        return levels;
    }

    /// <summary>Em um desenho: de cada ocorrência do grau até a próxima (uma oitava), subindo e descendo.</summary>
    static IEnumerable<ScaleNote> OctaveCut(List<ScaleNote> shape, string degree)
    {
        var marks = shape.Select((n, i) => (n, i)).Where(x => x.n.Degree == degree).Select(x => x.i).ToList();
        for (int k = 0; k + 1 < marks.Count; k++)
        {
            var segment = shape.GetRange(marks[k], marks[k + 1] - marks[k] + 1);
            foreach (var note in UpAndDown(segment)) yield return note;
        }
    }

    // Capítulo 4: dois desenhos vizinhos; a troca acontece em cada corda, uma por vez.
    static List<Level> Transitions()
    {
        var levels = new List<Level>();
        foreach (var quality in new[] { ScaleQuality.Minor, ScaleQuality.Major })
        {
            var shapes = Pentatonic.ShapesUpTheNeck(Sol, quality);
            var order = Pentatonic.ShapeOrder(quality);
            for (int i = 0; i + 1 < shapes.Count; i++)
            {
                var (lower, upper) = (shapes[i], shapes[i + 1]);
                var union = Distinct(lower.Concat(upper));
                string pair = $"Desenhos {order[i]} → {order[i + 1]}";
                var up = Enumerable.Range(0, 6).Select(k => 6 - k).SelectMany(s => TransitionUp(lower, upper, s));
                var down = Enumerable.Range(1, 6).SelectMany(s => TransitionDown(lower, upper, s));
                levels.Add(new Level(pair, $"{KeyName(quality)} · grave → agudo", Sol, quality, union, Chart(up)));
                levels.Add(new Level(pair, $"{KeyName(quality)} · agudo → grave", Sol, quality, union, Chart(down)));
            }
        }
        return levels;
    }

    /// <summary>Sobe no desenho de baixo até a corda da troca, passa para o de cima nessa corda e desce todo o de cima.</summary>
    static IEnumerable<ScaleNote> TransitionUp(List<ScaleNote> lower, List<ScaleNote> upper, int changeString)
    {
        var path = lower.Where(n => n.String > changeString).ToList();
        path.AddRange(lower.Concat(upper).Where(n => n.String == changeString).DistinctBy(n => n.Midi).OrderBy(n => n.Midi));
        path.AddRange(upper.Where(n => n.String < changeString));
        path.AddRange(Enumerable.Reverse(upper).Skip(1));
        return path;
    }

    /// <summary>O inverso: desce no desenho de cima até a corda da troca, passa para o de baixo e sobe todo o de baixo.</summary>
    static IEnumerable<ScaleNote> TransitionDown(List<ScaleNote> lower, List<ScaleNote> upper, int changeString)
    {
        var path = Enumerable.Reverse(upper).Where(n => n.String < changeString).ToList();
        path.AddRange(lower.Concat(upper).Where(n => n.String == changeString).DistinctBy(n => n.Midi).OrderByDescending(n => n.Midi));
        path.AddRange(Enumerable.Reverse(lower).Where(n => n.String > changeString));
        path.AddRange(lower.Skip(1));
        return path;
    }

    // Capítulo 5: só duas cordas, atravessando os 5 desenhos na horizontal (subindo e depois descendo o braço).
    static List<Level> StringPairs()
    {
        var levels = new List<Level>();
        foreach (var quality in new[] { ScaleQuality.Minor, ScaleQuality.Major })
        {
            var shapes = Pentatonic.ShapesUpTheNeck(Sol, quality);
            for (int high = 1; high <= 5; high++)
            {
                int low = high + 1;
                var up = shapes.SelectMany(shape => shape.Where(n => n.String == low || n.String == high)).ToList();
                levels.Add(new Level($"Cordas {low} e {high}", $"{KeyName(quality)} · sobe e desce o braço", Sol, quality,
                    Distinct(up), Chart(UpAndDown(up))));
            }
        }
        return levels;
    }
}
