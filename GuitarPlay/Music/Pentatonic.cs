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

    static readonly Dictionary<int, string> MinorDegrees = new() { [0] = "1", [3] = "b3", [5] = "4", [7] = "5", [10] = "b7" };
    static readonly Dictionary<int, string> MajorDegrees = new() { [0] = "1", [2] = "2", [4] = "3", [7] = "5", [9] = "6" };

    public static List<ScaleNote> Shape(int tonicPc, ScaleQuality quality, int shape)
    {
        int minorRootPc = quality == ScaleQuality.Minor ? tonicPc : MathUtil.Mod(tonicPc + 9, 12);
        int rootFret = MathUtil.Mod(minorRootPc - Tuning.OpenPitch(6), 12);
        int startFret = rootFret + MinorIntervals[shape - 1];
        if (startFret > 14) startFret -= 12;

        int pitch = Tuning.OpenPitch(6) + startFret;
        var notes = new List<ScaleNote>(12);
        for (int i = 0; i < 12; i++)
        {
            int guitarString = 6 - i / 2;
            notes.Add(new ScaleNote(guitarString, pitch - Tuning.OpenPitch(guitarString), pitch, DegreeOf(pitch, tonicPc, quality)));
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

    static string DegreeOf(int pitch, int tonicPc, ScaleQuality quality)
    {
        int interval = MathUtil.Mod(pitch - tonicPc, 12);
        return (quality == ScaleQuality.Minor ? MinorDegrees : MajorDegrees)[interval];
    }
}
