using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

public sealed class MenuScreen : IScreen
{
    public static readonly int[] BpmOptions = { 60, 80, 100, 120 };

    readonly GuitarPlayGame _game;
    readonly IReadOnlyList<Level> _levels = LevelFactory.World1;
    readonly FretboardView _fretboard = new(new Rectangle(40, 548, 1200, 160));
    int _selected;
    int _bpmIndex;
    double _time;

    public MenuScreen(GuitarPlayGame game, int selected = 0, int bpmIndex = 0)
    {
        _game = game;
        _selected = selected;
        _bpmIndex = bpmIndex;
    }

    public void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        if (_game.Pressed(Keys.Escape)) _game.Exit();
        if (_game.Pressed(Keys.Up)) _selected = MathUtil.Mod(_selected - 1, _levels.Count);
        if (_game.Pressed(Keys.Down)) _selected = MathUtil.Mod(_selected + 1, _levels.Count);
        if (_game.Pressed(Keys.Left)) _bpmIndex = Math.Max(0, _bpmIndex - 1);
        if (_game.Pressed(Keys.Right)) _bpmIndex = Math.Min(BpmOptions.Length - 1, _bpmIndex + 1);
        if (_game.Pressed(Keys.Enter) || _game.Pressed(Keys.Space))
            _game.ShowScreen(new PlayScreen(_game, _selected, _bpmIndex));
    }

    public void Draw(GameTime gameTime)
    {
        var p = _game.Primitives;
        _game.DrawBackground();
        for (int i = 0; i < _levels.Count; i++)
        {
            float y = 150 + i * 62;
            if (i == _selected)
            {
                p.Rect(60, y, 560, 54, Palette.Panel);
                p.Rect(60, y, 5, 54, Palette.Gold);
            }
        }
        _fretboard.DrawBase(p);
        p.Flush();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

        _game.Text("GUITAR PLAY", new Vector2(60, 34), 60, Palette.Gold, bold: true, shadow: true);
        _game.Text("Mundo 1 · Os 5 desenhos da pentatônica", new Vector2(62, 104), 20, Palette.TextDim);

        for (int i = 0; i < _levels.Count; i++)
        {
            float y = 150 + i * 62;
            var color = i == _selected ? Palette.Text : Palette.TextDim;
            _game.Text(_levels[i].Title, new Vector2(84, y + 6), 24, color, bold: true);
            _game.Text(_levels[i].Subtitle, new Vector2(250, y + 13), 17, Palette.TextFaint);
        }

        float x = 700;
        _game.Text("ANDAMENTO", new Vector2(x, 150), 15, Palette.TextFaint, bold: true);
        _game.Text($"◀  {BpmOptions[_bpmIndex]} BPM  ▶", new Vector2(x, 172), 38, Palette.Text, bold: true);

        _game.Text("CONTROLES", new Vector2(x, 250), 15, Palette.TextFaint, bold: true);
        string[] help =
        {
            "↑ ↓   escolher o nível",
            "← →   mudar o BPM",
            "Enter   começar",
            "Esc   sair",
            "Na pista: teclas 1 a 6 ou A S D F G H",
            "tocam as cordas (6ª corda à esquerda).",
        };
        for (int i = 0; i < help.Length; i++)
            _game.Text(help[i], new Vector2(x, 274 + i * 28), 19, Palette.TextDim);

        var level = _levels[_selected];
        _game.Text($"{level.Title} · {level.Subtitle.Split(" · ")[0]}", new Vector2(40, 518), 18, Palette.Text, bold: true);
        _fretboard.DrawSprites(_game, level.ShapeNotes, null, _time);

        _game.Batch.End();
    }
}
