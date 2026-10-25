namespace GuitarPlay.Music;

public enum ScaleQuality { Minor, Major }

public readonly record struct ScaleNote(int String, int Fret, int Midi, string Degree);

public static class Tuning
{
    // Afinação padrão (MIDI). Índice 0 = corda 1 (Mi agudo) ... índice 5 = corda 6 (Mi grave).
    static readonly int[] OpenMidi = { 64, 59, 55, 50, 45, 40 };

    public static int OpenPitch(int guitarString) => OpenMidi[guitarString - 1];
}

public static class NoteNames
{
    static readonly string[] Names = { "Dó", "Dó#", "Ré", "Mib", "Mi", "Fá", "Fá#", "Sol", "Láb", "Lá", "Sib", "Si" };

    public static string Of(int pitchClass) => Names[MathUtil.Mod(pitchClass, 12)];
}

/// <summary>
/// Gera os 5 desenhos da pentatônica, duas notas por corda.
/// Desenho 1 começa na tônica da menor (na 6ª corda); os mesmos desenhos servem
/// para a maior relativa, só muda qual nota é a tônica (Sol maior começa no Desenho 2).
/// </summary>
public static class Pentatonic
{
    static readonly int[] MinorIntervals = { 0, 3, 5, 7, 10 };

    // Nome de cada intervalo a partir da tônica (vale para menor, maior e notas acrescentadas).
    static readonly string[] ChromaticDegrees = { "1", "b2", "2", "b3", "3", "4", "#4", "5", "b6", "6", "b7", "7" };

    /// <summary>Os graus da escala, da tônica para cima.</summary>
    public static IReadOnlyList<string> Degrees(ScaleQuality quality) =>
        quality == ScaleQuality.Minor ? new[] { "1", "b3", "4", "5", "b7" } : new[] { "1", "2", "3", "5", "6" };

    public static string DegreeName(string degree) => degree switch
    {
        "1" => "tônica",
        "2" => "segunda maior",
        "b3" => "terça menor",
        "3" => "terça maior",
        "4" => "quarta justa",
        "5" => "quinta justa",
        "6" => "sexta maior",
        "b7" => "sétima menor",
        "#4" => "quarta aumentada (blue note)",
        "b2" => "segunda menor",
        "b6" => "sexta menor",
        "7" => "sétima maior",
        _ => degree,
    };

    /// <summary>
    /// Ordem dos desenhos subindo o braço. Na menor começa no Desenho 1; na maior, no Desenho 2
    /// (é onde a tônica maior fica na 6ª corda).
    /// </summary>
    public static int[] ShapeOrder(ScaleQuality quality) =>
        quality == ScaleQuality.Minor ? new[] { 1, 2, 3, 4, 5 } : new[] { 2, 3, 4, 5, 1 };

    /// <summary>Os 5 desenhos na ordem do braço, cada um acima do anterior.</summary>
    public static List<List<ScaleNote>> ShapesUpTheNeck(int tonicPc, ScaleQuality quality)
    {
        var shapes = new List<List<ScaleNote>>();
        foreach (int shape in ShapeOrder(quality))
            shapes.Add(shapes.Count == 0 ? Shape(tonicPc, quality, shape) : Shape(tonicPc, quality, shape, shapes[^1][0].Fret + 1));
        return shapes;
    }

    /// <summary>Notas do desenho, da mais grave para a mais aguda (duas por corda).</summary>
    /// <param name="minStartFret">Se informado, o desenho é posto na primeira oitava que começa nessa casa ou depois.</param>
    public static List<ScaleNote> Shape(int tonicPc, ScaleQuality quality, int shape, int minStartFret = -1)
    {
        int minorRootPc = quality == ScaleQuality.Minor ? tonicPc : MathUtil.Mod(tonicPc + 9, 12);
        int rootFret = MathUtil.Mod(minorRootPc - Tuning.OpenPitch(6), 12);
        int startFret = rootFret + MinorIntervals[shape - 1];
        if (minStartFret < 0)
        {
            if (startFret > 14) startFret -= 12;
        }
        else
        {
            while (startFret < minStartFret) startFret += 12;
            while (startFret - 12 >= minStartFret) startFret -= 12;
        }

        int pitch = Tuning.OpenPitch(6) + startFret;
        var notes = new List<ScaleNote>(12);
        for (int i = 0; i < 12; i++)
        {
            int guitarString = 6 - i / 2;
            notes.Add(new ScaleNote(guitarString, pitch - Tuning.OpenPitch(guitarString), pitch, DegreeOf(pitch, tonicPc)));
            pitch = NextInScale(pitch, minorRootPc);
        }
        return notes;
    }

    static int NextInScale(int pitch, int minorRootPc)
    {
        do pitch++;
        while (Array.IndexOf(MinorIntervals, MathUtil.Mod(pitch - minorRootPc, 12)) < 0);
        return pitch;
    }

    public static string DegreeOf(int pitch, int tonicPc) => ChromaticDegrees[MathUtil.Mod(pitch - tonicPc, 12)];

    /// <summary>A próxima nota da pentatônica acima de uma altura (destino de um bend).</summary>
    public static int NextScalePitch(int pitch, int tonicPc, ScaleQuality quality) =>
        NextInScale(pitch, quality == ScaleQuality.Minor ? tonicPc : MathUtil.Mod(tonicPc + 9, 12));

    /// <summary>
    /// Acrescenta graus da escala diatônica ou do modo dentro da região do desenho (ex.: 2 e b6 no Eólio).
    /// Cada nota nova vai para a corda em que fica entre as notas que já existem ali, ou o mais perto delas,
    /// sem sair da região do desenho.
    /// </summary>
    public static List<ScaleNote> AddDegrees(IReadOnlyList<ScaleNote> shape, int tonicPc, params int[] intervals)
    {
        int minFret = shape.Min(n => n.Fret), maxFret = shape.Max(n => n.Fret);
        var notes = shape.ToList();
        // Procura um pouco abaixo e acima das pontas: a escala CAGED pode começar antes da tônica (ex.: F# na 6ª corda).
        for (int pitch = shape.Min(n => n.Midi) - 2; pitch <= shape.Max(n => n.Midi) + 2; pitch++)
        {
            if (!intervals.Contains(MathUtil.Mod(pitch - tonicPc, 12))) continue;
            ScaleNote? best = null;
            int bestScore = int.MaxValue;
            for (int s = 6; s >= 1; s--)
            {
                int fret = pitch - Tuning.OpenPitch(s);
                if (fret < Math.Max(0, minFret - 1) || fret > maxFret + 1) continue;
                var frets = shape.Where(n => n.String == s).Select(n => n.Fret).ToList();
                int score = frets.Count == 0 ? 10
                    : fret < frets.Min() ? frets.Min() - fret
                    : fret > frets.Max() ? fret - frets.Max() : 0;
                if (fret < minFret || fret > maxFret) score++;
                if (score >= bestScore) continue;
                bestScore = score;
                best = new ScaleNote(s, fret, pitch, DegreeOf(pitch, tonicPc));
            }
            if (best is { } note) notes.Add(note);
        }
        return notes.OrderBy(n => n.Midi).ThenByDescending(n => n.String).ToList();
    }

    /// <summary>
    /// Acrescenta uma nota ao lado de cada ocorrência de um grau, na mesma corda
    /// (ex.: terça maior um semitom acima da b3; blue note um semitom acima da 4).
    /// </summary>
    public static List<ScaleNote> AddNextTo(IEnumerable<ScaleNote> shape, int tonicPc, string anchorDegree, int semitones)
    {
        var notes = shape.ToList();
        var added = notes.Where(n => n.Degree == anchorDegree)
            .Select(n => new ScaleNote(n.String, n.Fret + semitones, n.Midi + semitones, DegreeOf(n.Midi + semitones, tonicPc)));
        return notes.Concat(added).OrderBy(n => n.Midi).ThenByDescending(n => n.String).ToList();
    }
}
