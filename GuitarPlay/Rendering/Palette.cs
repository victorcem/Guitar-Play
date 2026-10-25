using Microsoft.Xna.Framework;

namespace GuitarPlay.Rendering;

/// <summary>Cores do jogo. Os graus seguem as cores do ebook: tônica vermelha, terça verde, quinta azul.</summary>
public static class Palette
{
    public static readonly Color BackgroundTop = new(24, 18, 44);
    public static readonly Color BackgroundBottom = new(6, 6, 12);
    public static readonly Color Panel = new(36, 34, 64);

    public static readonly Color Root = new(232, 65, 74);
    public static readonly Color Third = new(46, 204, 113);
    public static readonly Color Fifth = new(52, 152, 235);
    public static readonly Color Other = new(245, 176, 65);
    public static readonly Color BlueNote = new(150, 110, 255);

    public static readonly Color Gold = new(255, 200, 60);
    public static readonly Color Text = new(240, 240, 250);
    public static readonly Color TextDim = new(170, 168, 200);
    public static readonly Color TextFaint = new(110, 108, 140);
    public static readonly Color GuitarString = new(200, 200, 215);
    public static readonly Color Missed = new(90, 90, 100);
    /// <summary>Nota sem pista de grau (níveis de ajuda mais altos).</summary>
    public static readonly Color Neutral = new(125, 124, 150);
    public static readonly Color Miss = new(235, 80, 80);

    public static Color ForDegree(string degree) => degree switch
    {
        "1" => Root,
        "b3" or "3" => Third,
        "5" => Fifth,
        "#4" => BlueNote,
        _ => Other,
    };
}
