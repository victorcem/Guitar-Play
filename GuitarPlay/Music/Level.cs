namespace GuitarPlay.Music;

/// <summary>Uma nota da pista: em que tempo (em batidas) ela chega e onde tocar.</summary>
/// <param name="Bend">Semitons de bend (0 = nota normal; 1 = meio tom, 2 = um tom...).</param>
public sealed record ChartNote(double Beat, int String, int Fret, int Midi, string Degree, int Bend = 0)
{
    /// <summary>A altura que precisa soar: a própria nota ou o destino do bend.</summary>
    public int TargetMidi => Midi + Bend;
}

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

    /// <summary>Um passo do exercício: a nota e, se houver, quantos semitons de bend.</summary>
    readonly record struct Step(ScaleNote Note, int Bend = 0);

    static IReadOnlyList<World>? _worlds;

    public static IReadOnlyList<World> Worlds => _worlds ??= new[]
    {
        new World("Os 5 desenhos", "Capítulo 1", ShapeLevels(ScaleQuality.Minor)),
        new World("Pentatônica maior", "Capítulo 2", ShapeLevels(ScaleQuality.Major).Append(MinorMajorSwitch()).ToList()),
        new World("Recortes de oitava", "Capítulo 3", OctaveCuts()),
        new World("Transições entre desenhos", "Capítulo 4", Transitions()),
        new World("Pares de cordas", "Capítulo 5", StringPairs()),
        new World("Bends", "Capítulo 6", Bends()),
        new World("Pentatônicas mistas", "Capítulo 7", MixedPentatonics()),
        new World("Blue note", "Capítulo 8", BlueNote()),
    };

    public static Level Get(LevelRef at) => Worlds[at.World].Levels[at.Level];

    static string KeyName(ScaleQuality quality) => $"{NoteNames.Of(Sol)} {(quality == ScaleQuality.Minor ? "menor" : "maior")}";

    static List<ChartNote> Chart(IEnumerable<ScaleNote> path) => Chart(path.Select(n => new Step(n)));

    /// <summary>Uma nota por batida; o bend ganha duas batidas para dar tempo de chegar na nota.</summary>
    static List<ChartNote> Chart(IEnumerable<Step> steps)
    {
        var chart = new List<ChartNote>();
        double beat = 0;
        foreach (var (note, bend) in steps)
        {
            chart.Add(new ChartNote(beat, note.String, note.Fret, note.Midi, note.Degree, bend));
            beat += bend > 0 ? 2 : 1;
        }
        return chart;
    }

    static List<ScaleNote> Distinct(IEnumerable<ScaleNote> notes) =>
        notes.DistinctBy(n => (n.String, n.Fret)).ToList();

    static IEnumerable<ScaleNote> UpAndDown(IReadOnlyList<ScaleNote> notes) => notes.Concat(notes.Reverse().Skip(1));

    static List<List<ScaleNote>> Shapes(ScaleQuality quality) => Pentatonic.ShapesUpTheNeck(Sol, quality);

    /// <summary>Pentatônica menor + blue note (#4, um semitom acima da 4), nos 5 desenhos.</summary>
    static List<List<ScaleNote>> BluesShapes() =>
        Shapes(ScaleQuality.Minor).Select(s => Pentatonic.AddNextTo(s, Sol, "4", 1)).ToList();

    // Capítulos 1 e 2: cada desenho sobe e desce, na ordem do braço (na maior, o Desenho 1 fica na casa 12).
    static List<Level> ShapeLevels(ScaleQuality quality) =>
        Shapes(quality).Zip(Pentatonic.ShapeOrder(quality), (notes, shape) =>
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
                levels.Add(OctaveCutLevel(Shapes(quality), degree, name, $"{KeyName(quality)} · todos os desenhos", quality));
        }
        return levels;
    }

    static Level OctaveCutLevel(List<List<ScaleNote>> shapes, string degree, string title, string subtitle, ScaleQuality quality) =>
        new(title, subtitle, Sol, quality, Distinct(shapes.SelectMany(s => s)), Chart(shapes.SelectMany(s => OctaveCut(s, degree))));

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
            levels.AddRange(TransitionLevels(Shapes(quality), Pentatonic.ShapeOrder(quality), KeyName(quality), quality, bothDirections: true));
        return levels;
    }

    static IEnumerable<Level> TransitionLevels(List<List<ScaleNote>> shapes, int[] order, string label, ScaleQuality quality, bool bothDirections)
    {
        for (int i = 0; i + 1 < shapes.Count; i++)
        {
            var (lower, upper) = (shapes[i], shapes[i + 1]);
            var union = Distinct(lower.Concat(upper));
            string pair = $"Desenhos {order[i]} → {order[i + 1]}";
            var up = Enumerable.Range(0, 6).Select(k => 6 - k).SelectMany(s => TransitionUp(lower, upper, s));
            yield return new Level(pair, $"{label} · grave → agudo", Sol, quality, union, Chart(up));
            if (!bothDirections) continue;
            var down = Enumerable.Range(1, 6).SelectMany(s => TransitionDown(lower, upper, s));
            yield return new Level(pair, $"{label} · agudo → grave", Sol, quality, union, Chart(down));
        }
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
            for (int high = 1; high <= 5; high++)
                levels.Add(StringPairLevel(Shapes(quality), high, $"{KeyName(quality)} · sobe e desce o braço", quality));
        return levels;
    }

    static Level StringPairLevel(List<List<ScaleNote>> shapes, int high, string subtitle, ScaleQuality quality)
    {
        int low = high + 1;
        var up = shapes.SelectMany(shape => shape.Where(n => n.String == low || n.String == high)).ToList();
        return new Level($"Cordas {low} e {high}", subtitle, Sol, quality, Distinct(up), Chart(UpAndDown(up)));
    }

    // Capítulo 6: em cada corda, bend da nota mais aguda até a próxima nota da escala.
    static List<Level> Bends()
    {
        var levels = new List<Level>();
        foreach (var (shape, number) in Shapes(ScaleQuality.Minor).Zip(Pentatonic.ShapeOrder(ScaleQuality.Minor)))
        {
            // Passo 1: primeiro a nota-alvo presa (para ouvir onde chegar), depois o bend até ela.
            var isolated = new List<Step>();
            for (int s = 6; s >= 1; s--)
            {
                var (top, semitones) = BendOn(shape, s);
                int target = top.Midi + semitones;
                isolated.Add(new Step(new ScaleNote(s, top.Fret + semitones, target, Pentatonic.DegreeOf(target, Sol))));
                isolated.Add(new Step(top, semitones));
            }
            levels.Add(new Level($"Desenho {number}", "Sol menor · bends isolados", Sol, ScaleQuality.Minor,
                Distinct(shape.Concat(isolated.Select(x => x.Note))), Chart(isolated)));

            // Passo 2: sobe o desenho e, ao chegar na corda da vez, faz o bend em vez de seguir (1ª corda, depois 2ª...).
            var inScale = new List<Step>();
            for (int s = 1; s <= 6; s++)
            {
                var notes = shape.Where(n => n.String >= s).ToList();
                inScale.AddRange(notes.SkipLast(1).Select(n => new Step(n)));
                inScale.Add(new Step(notes[^1], BendOn(shape, s).Semitones));
            }
            levels.Add(new Level($"Desenho {number}", "Sol menor · bends na escala", Sol, ScaleQuality.Minor, shape, Chart(inScale)));
        }
        return levels;
    }

    /// <summary>A nota mais aguda da corda e quantos semitons até a próxima nota da escala.</summary>
    static (ScaleNote Top, int Semitones) BendOn(List<ScaleNote> shape, int guitarString)
    {
        var top = shape.Where(n => n.String == guitarString).MaxBy(n => n.Midi);
        return (top, Pentatonic.NextScalePitch(top.Midi, Sol, ScaleQuality.Minor) - top.Midi);
    }

    // Capítulo 7: versão 1 = menor + terça maior (ao lado da b3); versão 2 = maior + terça menor (ao lado da 3).
    static List<Level> MixedPentatonics()
    {
        var minorShapes = Shapes(ScaleQuality.Minor).Select(s => Pentatonic.AddNextTo(s, Sol, "b3", 1)).ToList();
        var majorShapes = Shapes(ScaleQuality.Major).Select(s => Pentatonic.AddNextTo(s, Sol, "3", -1)).ToList();
        var levels = new List<Level>();
        foreach (var (shape, number) in minorShapes.Zip(Pentatonic.ShapeOrder(ScaleQuality.Minor)))
            levels.Add(new Level($"Desenho {number}", "Sol menor + 3 (versão 1)", Sol, ScaleQuality.Minor, shape, Chart(UpAndDown(shape))));
        foreach (var (shape, number) in majorShapes.Zip(Pentatonic.ShapeOrder(ScaleQuality.Major)))
            levels.Add(new Level($"Desenho {number}", "Sol maior + b3 (versão 2)", Sol, ScaleQuality.Major, shape, Chart(UpAndDown(shape))));

        // Passo 3: as duas versões lado a lado, na mesma região do braço.
        var v1 = minorShapes[0];
        var v2 = majorShapes[0];
        levels.Add(new Level("Comparar versões", "versão 1 (Desenho 1) → versão 2 (Desenho 2)", Sol, ScaleQuality.Minor,
            Distinct(v1.Concat(v2)), Chart(UpAndDown(v1).Concat(UpAndDown(v2)))));
        return levels;
    }

    // Capítulo 8: a blue note em cada desenho e nos exercícios dos capítulos 3, 4 e 5, mais os bends de meio tom.
    static List<Level> BlueNote()
    {
        var shapes = BluesShapes();
        var order = Pentatonic.ShapeOrder(ScaleQuality.Minor);
        var levels = shapes.Zip(order, (shape, number) =>
            new Level($"Desenho {number}", "Sol menor + blue note", Sol, ScaleQuality.Minor, shape, Chart(UpAndDown(shape)))).ToList();

        levels.Add(OctaveCutLevel(shapes, "1", "Tônica a tônica", "blue note · todos os desenhos", ScaleQuality.Minor));
        levels.AddRange(TransitionLevels(shapes, order, "blue note", ScaleQuality.Minor, bothDirections: false));
        levels.Add(StringPairLevel(shapes, 1, "blue note · sobe e desce o braço", ScaleQuality.Minor));
        levels.Add(StringPairLevel(shapes, 2, "blue note · sobe e desce o braço", ScaleQuality.Minor));

        // Dica do livro: bend de meio tom da 4 até a blue note (tensão) e da blue note até a 5 (resolução).
        var bends = new List<Step>();
        foreach (var shape in shapes)
            for (int i = 0; i + 2 < shape.Count; i++)
            {
                if (shape[i].Degree != "4" || shape[i + 1].Degree != "#4") continue;
                bends.Add(new Step(shape[i], 1));
                bends.Add(new Step(shape[i + 1]));
                bends.Add(new Step(shape[i + 1], 1));
                bends.Add(new Step(shape[i + 2]));
            }
        levels.Add(new Level("Bends na blue note", "4 → #4 e #4 → 5 · todos os desenhos", Sol, ScaleQuality.Minor,
            Distinct(shapes.SelectMany(s => s)), Chart(bends)));
        return levels;
    }
}
