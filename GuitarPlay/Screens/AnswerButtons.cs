using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

/// <summary>Os 4 botões de grau (teclas 1 a 4 ou clique) usados nos quizzes.</summary>
sealed class AnswerButtons
{
    const int Width = 270, Height = 76, Gap = 20, Y = 590;
    static readonly Keys[][] Shortcuts =
    {
        new[] { Keys.D1, Keys.NumPad1 }, new[] { Keys.D2, Keys.NumPad2 },
        new[] { Keys.D3, Keys.NumPad3 }, new[] { Keys.D4, Keys.NumPad4 },
    };

    readonly string[] _options;

    public AnswerButtons(IEnumerable<string> options) => _options = options.ToArray();

    static Rectangle Rect(int i)
    {
        const int total = 4 * Width + 3 * Gap;
        int x = (GuitarPlayGame.Width - total) / 2 + i * (Width + Gap);
        return new Rectangle(x, Y, Width, Height);
    }

    /// <summary>O grau escolhido neste quadro, se houver.</summary>
    public string? Poll(GuitarPlayGame game)
    {
        for (int i = 0; i < _options.Length; i++)
            if (Shortcuts[i].Any(game.Pressed) || (game.Clicked && Rect(i).Contains(game.MousePosition)))
                return _options[i];
        return null;
    }

    /// <param name="answer">Resposta dada (null enquanto espera).</param>
    /// <param name="correct">Grau certo, pintado de verde depois da resposta.</param>
    public void DrawBase(GuitarPlayGame game, string? answer, string correct)
    {
        for (int i = 0; i < _options.Length; i++)
        {
            var rect = Rect(i);
            var color = Palette.Panel;
            if (answer is not null && _options[i] == correct) color = Palette.Third * 0.8f;
            else if (answer == _options[i]) color = Palette.Miss * 0.8f;
            else if (answer is null && rect.Contains(game.MousePosition)) color = Palette.Panel * 1.5f;
            game.Primitives.Rect(rect.X, rect.Y, rect.Width, rect.Height, color);
            game.Primitives.Rect(rect.X, rect.Y, 5, rect.Height, Palette.ForDegree(_options[i]));
        }
    }

    public void DrawText(GuitarPlayGame game, string? answer, string correct)
    {
        for (int i = 0; i < _options.Length; i++)
        {
            var rect = Rect(i);
            // Botão pintado de verde/vermelho na resposta: texto branco para manter o contraste.
            bool painted = answer is not null && (_options[i] == correct || _options[i] == answer);
            game.Text((i + 1).ToString(), new Vector2(rect.X + 22, rect.Y + rect.Height / 2f), 20,
                painted ? Color.White : Palette.TextFaint, bold: true, middle: true);
            game.Text(_options[i], new Vector2(rect.X + 52, rect.Y + 8), 32, painted ? Color.White : Palette.ForDegree(_options[i]), bold: true);
            game.Text(Pentatonic.DegreeName(_options[i]), new Vector2(rect.X + 52, rect.Y + 48), 16, painted ? Color.White : Palette.TextDim);
        }
    }
}
