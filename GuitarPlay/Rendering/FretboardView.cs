using GuitarPlay.Music;
using Microsoft.Xna.Framework;

namespace GuitarPlay.Rendering;

/// <summary>Uma bolinha no braço: posição, cor e rótulo opcional.</summary>
public readonly record struct FretMark(int String, int Fret, Color Color, string? Label = null);

/// <summary>Braço da guitarra (como os diagramas do livro): corda 1 em cima, corda 6 embaixo.</summary>
public sealed class FretboardView
{
    // 18 casas: o bend do Desenho 5 pede a nota-alvo presa na casa 18.
    public const int Frets = 18;
    static readonly int[] SingleInlays = { 3, 5, 7, 9, 15, 17 };

    readonly Rectangle _area;

    public FretboardView(Rectangle area) => _area = area;

    float NutX => _area.X + 40;
    float FretWidth => (_area.Right - 10 - NutX) / Frets;
    float StringSpacing => (_area.Height - 44) / 5f;
    float Top => StringY(1) - 12;
    float Bottom => StringY(6) + 12;

    /// <summary>Raio das bolinhas: cresce com o tamanho do braço.</summary>
    public float NoteRadius => Math.Clamp(StringSpacing * 0.42f, 11f, 21f);

    float FretLineX(int fret) => NutX + fret * FretWidth;
    float NoteX(int fret) => fret == 0 ? NutX - 18 : NutX + (fret - 0.5f) * FretWidth;
    float StringY(int guitarString) => _area.Y + 14 + (guitarString - 1) * StringSpacing;

    public Vector2 Position(int guitarString, int fret) => new(NoteX(fret), StringY(guitarString));

    /// <summary>Corda e casa sob um ponto da tela, ou null se estiver fora do braço.</summary>
    public (int String, int Fret)? HitTest(Vector2 point)
    {
        int guitarString = (int)MathF.Round((point.Y - StringY(1)) / StringSpacing) + 1;
        if (guitarString is < 1 or > 6 || MathF.Abs(point.Y - StringY(guitarString)) > StringSpacing / 2) return null;
        if (point.X < NutX - 36 || point.X > FretLineX(Frets)) return null;
        int fret = point.X < NutX ? 0 : (int)((point.X - NutX) / FretWidth) + 1;
        return (guitarString, fret);
    }

    public void DrawBase(Primitives p)
    {
        p.Rect(NutX, Top, FretLineX(Frets) - NutX, Bottom - Top, new Color(48, 32, 26));
        for (int fret = 1; fret <= Frets; fret++)
            p.Rect(FretLineX(fret) - 1.5f, Top, 3, Bottom - Top, new Color(150, 150, 160));
        p.Rect(NutX - 5, Top, 7, Bottom - Top, new Color(230, 222, 200));
        for (int s = 1; s <= 6; s++)
        {
            float thickness = 1f + (s - 1) * 0.45f;
            p.Rect(NutX - 24, StringY(s) - thickness / 2, FretLineX(Frets) - NutX + 24, thickness, Palette.GuitarString * 0.8f);
        }
    }

    /// <summary>Marcadores de casa e números. Chamar antes das bolinhas.</summary>
    public void DrawInlays(GuitarPlayGame game)
    {
        float midY = (StringY(3) + StringY(4)) / 2;
        foreach (int fret in SingleInlays)
            game.Circle(new Vector2(NoteX(fret), midY), 6, Color.White * 0.18f);
        game.Circle(new Vector2(NoteX(12), (StringY(2) + StringY(3)) / 2), 6, Color.White * 0.18f);
        game.Circle(new Vector2(NoteX(12), (StringY(4) + StringY(5)) / 2), 6, Color.White * 0.18f);

        for (int fret = 1; fret <= Frets; fret++)
            game.Text(fret.ToString(), new Vector2(NoteX(fret), Bottom + 4), 13, Palette.TextFaint, align: Align.Center);
    }

    public void DrawMark(GuitarPlayGame game, FretMark mark, float radiusScale = 1f)
    {
        var pos = Position(mark.String, mark.Fret);
        float radius = NoteRadius * radiusScale;
        game.Circle(pos, radius, mark.Color);
        if (mark.Label is not null)
            game.Text(mark.Label, pos, radius * 1.1f, new Color(15, 15, 25), bold: true, align: Align.Center, middle: true);
    }

    public void DrawPulse(GuitarPlayGame game, int guitarString, int fret, double time, Color? color = null)
    {
        float pulse = 0.6f + 0.4f * MathF.Sin((float)time * 10f);
        game.Ring(Position(guitarString, fret), NoteRadius + 6, (color ?? Color.White) * pulse);
    }

    /// <summary>Desenho completo com os graus coloridos e, opcionalmente, a próxima nota pulsando.</summary>
    public void DrawSprites(GuitarPlayGame game, IEnumerable<ScaleNote> notes, (int String, int Fret)? highlight, double time,
        IReadOnlyList<string>? extras = null)
    {
        DrawInlays(game);
        foreach (var note in notes)
            DrawMark(game, new FretMark(note.String, note.Fret, Palette.ForDegree(note.Degree, extras), note.Degree));
        if (highlight is { } h) DrawPulse(game, h.String, h.Fret, time);
    }
}
