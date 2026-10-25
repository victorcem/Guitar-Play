using GuitarPlay.Gameplay;
using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

public sealed class MenuScreen : IScreen
{
    const float ListY = 210, RowHeight = 58;
    static readonly Activity[] Activities = MenuState.PerShape;
    static readonly Rectangle TrainingPanel = new(880, 474, 340, 56);

    readonly GuitarPlayGame _game;
    readonly IReadOnlyList<Level> _levels = LevelFactory.World1;
    readonly FretboardView _fretboard = new(new Rectangle(40, 548, 1200, 160));
    MenuState _state;
    double _time;

    public MenuScreen(GuitarPlayGame game, MenuState? state = null)
    {
        _game = game;
        _state = state ?? new MenuState();
        if (_state.Activity == Activity.Training) _state = _state with { Activity = Activity.Constellations };
        ClampAssist();
    }

    Progress Progress => _game.Progress;

    /// <summary>Ao trocar de desenho, a ajuda escolhida pode ainda não estar liberada nele.</summary>
    void ClampAssist()
    {
        int assist = _state.Assist;
        while (assist > 1 && !MenuState.IsAssistUnlocked(Progress, _state.LevelIndex, assist)) assist--;
        _state = _state with { Assist = assist };
    }

    public void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        int activity = Array.IndexOf(Activities, _state.Activity);
        if (_game.Pressed(Keys.Escape)) _game.Exit();
        if (_game.Pressed(Keys.Up)) _state = _state with { LevelIndex = MathUtil.Mod(_state.LevelIndex - 1, _levels.Count) };
        if (_game.Pressed(Keys.Down)) _state = _state with { LevelIndex = MathUtil.Mod(_state.LevelIndex + 1, _levels.Count) };
        if (_game.Pressed(Keys.Left)) _state = _state with { Activity = Activities[Math.Max(0, activity - 1)] };
        if (_game.Pressed(Keys.Right)) _state = _state with { Activity = Activities[Math.Min(Activities.Length - 1, activity + 1)] };
        if (_game.Pressed(Keys.OemMinus) || _game.Pressed(Keys.Subtract))
            _state = _state with { BpmIndex = Math.Max(0, _state.BpmIndex - 1) };
        if (_game.Pressed(Keys.OemPlus) || _game.Pressed(Keys.Add))
            _state = _state with { BpmIndex = Math.Min(MenuState.BpmOptions.Length - 1, _state.BpmIndex + 1) };
        if (_game.Pressed(Keys.Tab))
        {
            int next = _state.Assist % MenuState.MaxAssist + 1;
            _state = _state with { Assist = MenuState.IsAssistUnlocked(Progress, _state.LevelIndex, next) ? next : 1 };
        }
        ClampAssist();

        if (_game.Pressed(Keys.A))
        {
            _game.ShowScreen(new AudioSetupScreen(_game, _state));
            return;
        }
        if (_game.Pressed(Keys.T) || (_game.Clicked && TrainingPanel.Contains(_game.MousePosition)))
            _game.ShowScreen(new TrainingScreen(_game, _state with { Activity = Activity.Training }));
        else if (_game.Pressed(Keys.Enter) || _game.Pressed(Keys.Space))
            _game.ShowScreen(_state.CreateScreen(_game));
    }

    static Rectangle ChipRect(int i) => new(60 + i * 200, 148, 188, 40);

    int StarsOf(int levelIndex) => Progress.Stars((_state with { LevelIndex = levelIndex }).ProgressKey);

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
        int due = TrainingPlan.DueCount(Progress);
        p.Rect(TrainingPanel.X, TrainingPanel.Y, TrainingPanel.Width, TrainingPanel.Height,
            TrainingPanel.Contains(_game.MousePosition) ? Palette.Panel * 1.5f : Palette.Panel);
        p.Rect(TrainingPanel.X, TrainingPanel.Y, 5, TrainingPanel.Height, due > 0 ? Palette.Gold : Palette.Third);
        _fretboard.DrawBase(p);
        p.Flush();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

        _game.Text("GUITAR PLAY", new Vector2(60, 30), 54, Palette.Gold, bold: true, shadow: true);
        _game.Text("Mundo 1 · Os 5 desenhos da pentatônica e seus intervalos", new Vector2(62, 96), 19, Palette.TextDim);
        DrawInputStatus();

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
            int stars = StarsOf(i);
            for (int s = 0; s < 3; s++)
                _game.Text("★", new Vector2(520 + s * 26, y + 10), 24, s < stars ? Palette.Gold : Palette.Panel * 1.6f);
        }

        DrawActivityPanel();
        DrawTrainingPanel(due);

        var level = _state.Level;
        _game.Text($"{level.Title} · {level.KeyName}", new Vector2(40, 518), 18, Palette.Text, bold: true);
        _fretboard.DrawSprites(_game, level.ShapeNotes, null, _time);

        _game.Batch.End();
    }

    void DrawInputStatus()
    {
        string status;
        Color color;
        if (_game.Input is { } input)
        {
            string name = input.DeviceName.Length > 32 ? input.DeviceName[..31] + "…" : input.DeviceName;
            var settings = _game.Settings;
            status = $"Guitarra: {name} · " + (settings.Calibrated ? $"{settings.LatencySeconds * 1000:0} ms" : "não calibrada");
            color = settings.Calibrated ? Palette.Third : Palette.Other;
        }
        else
        {
            status = _game.InputError is null ? "Entrada: teclado" : "Guitarra desconectada · usando o teclado";
            color = _game.InputError is null ? Palette.TextDim : Palette.Other;
        }
        _game.Text($"{status}   (A: áudio)", new Vector2(1220, 44), 16, color, align: Align.Right);
    }

    void DrawActivityPanel()
    {
        const float x = 880;
        _game.Text(MenuState.NameOf(_state.Activity).ToUpperInvariant(), new Vector2(x, 214), 15, Palette.Gold, bold: true);
        var description = MenuState.DescriptionOf(_state.Activity);
        for (int i = 0; i < description.Length; i++)
            _game.Text(description[i], new Vector2(x, 238 + i * 25), 17, Palette.TextDim);

        string[] help;
        if (_state.Activity == Activity.Track)
        {
            _game.Text($"Andamento:  −  {_state.Bpm} BPM  +", new Vector2(x, 322), 20, Palette.Text, bold: true);
            _game.Text($"Ajuda {_state.Assist}: {MenuState.AssistName(_state.Assist)}", new Vector2(x, 350), 20, Palette.Text, bold: true);
            int next = _state.Assist + 1;
            if (next <= MenuState.MaxAssist && !MenuState.IsAssistUnlocked(Progress, _state.LevelIndex, next))
                _game.Text($"Ajuda {next} abre com {StarRating.ToPass}★ na Ajuda {_state.Assist}", new Vector2(x, 378), 15, Palette.TextFaint);
            help = new[] { "↑ ↓ desenho  ·  ← → atividade", "+ − BPM  ·  Tab ajuda  ·  Enter começar" };
        }
        else
            help = new[] { "↑ ↓ desenho  ·  ← → atividade", "Enter começar  ·  Esc sair" };
        for (int i = 0; i < help.Length; i++)
            _game.Text(help[i], new Vector2(x, 410 + i * 24), 16, Palette.TextFaint);
    }

    void DrawTrainingPanel(int due)
    {
        var panel = TrainingPanel;
        _game.Text("TREINO DO DIA  (T)", new Vector2(panel.X + 18, panel.Y + 8), 15, Palette.Gold, bold: true);
        int fresh = TrainingPlan.NewCount(Progress);
        string status = due > 0 ? $"{due} intervalos para revisar hoje"
            : fresh > 0 ? $"Nada vencido · {fresh} intervalos novos"
            : "Tudo em dia!";
        _game.Text(status, new Vector2(panel.X + 18, panel.Y + 28), 16, Palette.TextDim);
    }
}
