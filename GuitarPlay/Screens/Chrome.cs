using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;

namespace GuitarPlay.Screens;

/// <summary>Cabeçalho e rodapé comuns às atividades.</summary>
static class Chrome
{
    public static void Header(GuitarPlayGame game, string activity, Level level)
    {
        game.Text(activity.ToUpperInvariant(), new Vector2(60, 28), 15, Palette.TextFaint, bold: true);
        game.Text($"{level.Title} · {level.KeyName}", new Vector2(60, 48), 34, Palette.Text, bold: true);
    }

    public static void Footer(GuitarPlayGame game, string text) =>
        game.Text(text, new Vector2(GuitarPlayGame.Width / 2f, 688), 16, Palette.TextFaint, align: Align.Center);

    /// <summary>Barra horizontal simples (fundo + preenchimento).</summary>
    public static void Bar(Primitives p, float x, float y, float width, float fraction, Color color)
    {
        p.Rect(x, y, width, 8, Palette.Panel);
        p.Rect(x, y, width * Math.Clamp(fraction, 0, 1), 8, color);
    }
}
