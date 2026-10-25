namespace GuitarPlay.Music;

/// <summary>
/// Descreve a "posição geográfica" de uma nota em relação à tônica, como o livro pede:
/// quantas cordas e quantas casas de distância.
/// </summary>
public static class FretboardGeometry
{
    /// <summary>A tônica do desenho mais próxima da nota (menos cordas e casas de distância).</summary>
    public static ScaleNote NearestRoot(IEnumerable<ScaleNote> shape, ScaleNote note) =>
        shape.Where(n => n.Degree == "1" && n != note)
            .OrderBy(r => Math.Abs(r.String - note.String) * 2 + Math.Abs(r.Fret - note.Fret))
            .ThenByDescending(r => r.String)
            .First();

    public static string Describe(ScaleNote root, ScaleNote note)
    {
        int strings = root.String - note.String;
        int frets = note.Fret - root.Fret;

        string where = strings switch
        {
            0 => "na mesma corda",
            1 => "na corda vizinha mais aguda",
            -1 => "na corda vizinha mais grave",
            > 0 => $"{strings} cordas mais agudas",
            _ => $"{-strings} cordas mais graves",
        };
        string distance = frets switch
        {
            0 => "na mesma casa",
            1 => "1 casa à frente",
            -1 => "1 casa atrás",
            > 0 => $"{frets} casas à frente",
            _ => $"{-frets} casas atrás",
        };
        return $"{where}, {distance}";
    }

    /// <summary>Verdadeiro se o caminho entre as duas notas atravessa da corda Sol para a Si.</summary>
    public static bool CrossesBString(ScaleNote a, ScaleNote b) =>
        Math.Min(a.String, b.String) <= 2 && Math.Max(a.String, b.String) >= 3;
}
