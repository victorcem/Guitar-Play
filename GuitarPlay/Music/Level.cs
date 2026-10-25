namespace GuitarPlay.Music;

/// <summary>Uma nota da pista: em que tempo (em batidas) ela chega e onde tocar.</summary>
public sealed record ChartNote(double Beat, int String, int Fret, int Midi, string Degree);

public sealed record Level(
    string Title,
    string Subtitle,
    int TonicPc,
    ScaleQuality Quality,
    IReadOnlyList<ScaleNote> ShapeNotes,
    IReadOnlyList<ChartNote> Notes)
{
    public string KeyName => $"{NoteNames.Of(TonicPc)} {(Quality == ScaleQuality.Minor ? "menor" : "maior")}";
}

public static class LevelFactory
{
    public const int Sol = 7;

    static IReadOnlyList<Level>? _world1;

    /// <summary>Mundo 1: os 5 desenhos da pentatônica de Sol menor.</summary>
    public static IReadOnlyList<Level> World1 =>
        _world1 ??= Enumerable.Range(1, 5).Select(shape => ShapeUpDown(Sol, ScaleQuality.Minor, shape)).ToList();

    /// <summary>Sobe o desenho inteiro e desce de volta, uma nota por batida.</summary>
    public static Level ShapeUpDown(int tonicPc, ScaleQuality quality, int shape)
    {
        var shapeNotes = Pentatonic.Shape(tonicPc, quality, shape);
        var path = shapeNotes.Concat(Enumerable.Reverse(shapeNotes).Skip(1));
        var chart = path.Select((n, i) => new ChartNote(i, n.String, n.Fret, n.Midi, n.Degree)).ToList();
        string key = $"{NoteNames.Of(tonicPc)} {(quality == ScaleQuality.Minor ? "menor" : "maior")}";
        return new Level($"Desenho {shape}", $"{key} · sobe e desce", tonicPc, quality, shapeNotes, chart);
    }
}
