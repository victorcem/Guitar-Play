using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

public sealed class MenuScreen : IScreen
{
    const float ListY = 210, RowHeight = 58;
    static readonly Activity[] Activities = Enum.GetValues<Activity>();

    readonly GuitarPlayGame _game;
    readonly IReadOnlyList<Level> _levels = LevelFactory.World1;
    readonly FretboardView _fretboard = new(new Rectangle(40, 548, 1200, 160));
    MenuState _state;
    double _time;

    public MenuScreen(GuitarPlayGame game, MenuState? state = null)
    {
        _game = game;
        _state = state ?? new MenuState();
    }

    public void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        int activity = (int)_state.Activity;
        if (_game.Pressed(Keys.Escape)) _game.Exit();
        if (_game.Pressed(Keys.Up)) _state = _state with { LevelIndex = MathUtil.Mod(_state.LevelIndex - 1, _levels.Count) };
        if (_game.Pressed(Keys.Down)) _state = _state with { LevelIndex = MathUtil.Mod(_state.LevelIndex + 1, _levels.Count) };
        if (_game.Pressed(Keys.Left)) _state = _state with { Activity = Activities[Math.Max(0, activity - 1)] };
        if (_game.Pressed(Keys.Right)) _state = _state with { Activity = Activities[Math.Min(Activities.Length - 1, activity + 1)] };
        if (_game.Pressed(Keys.OemMinus) || _game.Pressed(Keys.Subtract))
            _state = _state with { BpmIndex = Math.Max(0, _state.BpmIndex - 1) };
        if (_game.Pressed(Keys.OemPlus) || _game.Pressed(Keys.Add))
            _state = _state with { BpmIndex = Math.Min(MenuState.BpmOptions.Length - 1, _state.BpmIndex + 1) };
        if (_game.Pressed(Keys.Enter) || _game.Pressed(Keys.Space))
            _game.ShowScreen(_state.CreateScreen(_game));
    }

    static Rectangle ChipRect(int i) => new(60 + i * 200, 148, 188, 40);

    public void Draw(GameTime gameTime)
    {
        var p = _game.Primitives;
        _game.DrawBackground();
        for (int i = 0; i < Activities.Length; i++)
        {
            var chip = ChipRect(i);
            p.Rect(chip.X, chip.Y, chip.Width, chip.Height, Activities[i] == _state.Activity ? Palette.Gold : Palette.Panel);
        }
        float rowY = ListY + _state.LevelIndex * RowHeight;
        p.Rect(60, rowY, 560, RowHeight - 6, Palette.Panel);
        p.Rect(60, rowY, 5, RowHeight - 6, Palette.Gold);
        _fretboard.DrawBase(p);
        p.Flush();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

        _game.Text("GUITAR PLAY", new Vector2(60, 30), 54, Palette.Gold, bold: true, shadow: true);
        _game.Text("Mundo 1 · Os 5 desenhos da pentatônica e seus intervalos", new Vector2(62, 96), 19, Palette.TextDim);

        for (int i = 0; i < Activities.Length; i++)
        {
            var chip = ChipRect(i);
            bool selected = Activities[i] == _state.Activity;
            _game.Text($"{i + 1}. {MenuState.NameOf(Activities[i])}", new Vector2(chip.Center.X, chip.Center.Y), 17,
                selected ? new Color(25, 20, 40) : Palette.TextDim, bold: true, align: Align.Center, middle: true);
        }

        for (int i = 0; i < _levels.Count; i++)
        {
            float y = ListY + i * RowHeight;
            var color = i == _state.LevelIndex ? Palette.Text : Palette.TextDim;
            _game.Text(_levels[i].Title, new Vector2(84, y + 10), 24, color, bold: true);
            _game.Text(_levels[i].KeyName, new Vector2(250, y + 17), 17, Palette.TextFaint);
        }

        const float x = 880;
        _game.Text(MenuState.NameOf(_state.Activity).ToUpperInvariant(), new Vector2(x, 214), 15, Palette.Gold, bold: true);
        var description = MenuState.DescriptionOf(_state.Activity);
        for (int i = 0; i < description.Length; i++)
            _game.Text(description[i], new Vector2(x, 238 + i * 25), 17, Palette.TextDim);

        if (_state.Activity == Activity.Track)
            _game.Text($"Andamento:  –  {_state.Bpm} BPM  +", new Vector2(x, 324), 22, Palette.Text, bold: true);

        string[] help = { "↑ ↓  desenho", "← →  atividade", "+ −  BPM da pista", "Enter  começar   ·   Esc  sair" };
        for (int i = 0; i < help.Length; i++)
            _game.Text(help[i], new Vector2(x, 380 + i * 26), 17, Palette.TextFaint);

        var level = _state.Level;
        _game.Text($"{level.Title} · {level.KeyName}", new Vector2(40, 518), 18, Palette.Text, bold: true);
        _fretboard.DrawSprites(_game, level.ShapeNotes, null, _time);

        _game.Batch.End();
    }
}
