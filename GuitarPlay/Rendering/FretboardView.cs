using GuitarPlay.Music;
using Microsoft.Xna.Framework;

namespace GuitarPlay.Rendering;

/// <summary>Mini braço da guitarra (como os diagramas do livro): corda 1 em cima, corda 6 embaixo.</summary>
public sealed class FretboardView
{
    public const int Frets = 17;
    static readonly int[] SingleInlays = { 3, 5, 7, 9, 15, 17 };

    readonly Rectangle _area;

    public FretboardView(Rectangle area) => _area = area;

    float NutX => _area.X + 40;
    float FretWidth => (_area.Right - 10 - NutX) / Frets;
    float Top => StringY(1) - 12;
    float Bottom => StringY(6) + 12;

    float FretLineX(int fret) => NutX + fret * FretWidth;
    float NoteX(int fret) => fret == 0 ? NutX - 18 : NutX + (fret - 0.5f) * FretWidth;
    float StringY(int guitarString) => _area.Y + 14 + (guitarString - 1) * (_area.Height - 44) / 5f;

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

    /// <param name="highlight">Corda e casa da próxima nota a tocar, se houver.</param>
    public void DrawSprites(GuitarPlayGame game, IEnumerable<ScaleNote> notes, (int String, int Fret)? highlight, double time)
    {
        float midY = (StringY(3) + StringY(4)) / 2;
        foreach (int fret in SingleInlays)
            game.Circle(new Vector2(NoteX(fret), midY), 6, Color.White * 0.18f);
        game.Circle(new Vector2(NoteX(12), (StringY(2) + StringY(3)) / 2), 6, Color.White * 0.18f);
        game.Circle(new Vector2(NoteX(12), (StringY(4) + StringY(5)) / 2), 6, Color.White * 0.18f);

        for (int fret = 1; fret <= Frets; fret++)
            game.Text(fret.ToString(), new Vector2(NoteX(fret), Bottom + 4), 13, Palette.TextFaint, align: Align.Center);

        foreach (var note in notes)
        {
            var pos = new Vector2(NoteX(note.Fret), StringY(note.String));
            game.Circle(pos, 11, Palette.ForDegree(note.Degree));
            game.Text(note.Degree, pos, 12, new Color(15, 15, 25), bold: true, align: Align.Center, middle: true);
        }

        if (highlight is { } h)
        {
            float pulse = 0.6f + 0.4f * MathF.Sin((float)time * 10f);
            game.Ring(new Vector2(NoteX(h.Fret), StringY(h.String)), 17, Color.White * pulse);
        }
    }
}
