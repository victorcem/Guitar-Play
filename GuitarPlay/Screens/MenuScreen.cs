using GuitarPlay.Gameplay;
using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

public sealed class MenuScreen : IScreen
{
    const float ListY = 200, RowHeight = 42;
    const int VisibleRows = 7;
    static readonly Activity[] Activities = MenuState.PerShape;
    static readonly Rectangle TrainingPanel = new(880, 474, 340, 56);

    readonly GuitarPlayGame _game;
    readonly FretboardView _fretboard = new(new Rectangle(40, 548, 1200, 160));
    MenuState _state;
    int _scroll;
    double _time;
    string _notice = "";
    double _noticeUntil;

    public MenuScreen(GuitarPlayGame game, MenuState? state = null)
    {
        _game = game;
        _state = state ?? new MenuState();
        if (_state.Activity == Activity.Training) _state = _state with { Activity = Activity.Constellations };
        Normalize();
    }

    Progress Progress => _game.Progress;
    IReadOnlyList<Level> Levels => _state.World.Levels;

    /// <summary>Mantém a escolha válida: atividade suportada, ajuda liberada e nível visível na lista.</summary>
    void Normalize()
    {
        if (!_state.Supports(_state.Activity)) _state = _state with { Activity = Activity.Track };
        int assist = _state.Assist;
        while (assist > 1 && !Unlocks.IsAssistUnlocked(Progress, _state.At, assist)) assist--;
        _state = _state with { Assist = assist };
        if (_state.LevelIndex < _scroll) _scroll = _state.LevelIndex;
        if (_state.LevelIndex >= _scroll + VisibleRows) _scroll = _state.LevelIndex - VisibleRows + 1;
    }

    void Notice(string text)
    {
        _notice = text;
        _noticeUntil = _time + 2.5;
    }

    public void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        if (_game.Pressed(Keys.Escape)) _game.Exit();

        int worlds = LevelFactory.Worlds.Count;
        if (_game.Pressed(Keys.Q) || _game.Pressed(Keys.PageUp))
            _state = _state with { WorldIndex = MathUtil.Mod(_state.WorldIndex - 1, worlds), LevelIndex = 0 };
        if (_game.Pressed(Keys.E) || _game.Pressed(Keys.PageDown))
            _state = _state with { WorldIndex = MathUtil.Mod(_state.WorldIndex + 1, worlds), LevelIndex = 0 };
        if (_game.Pressed(Keys.Up)) _state = _state with { LevelIndex = MathUtil.Mod(_state.LevelIndex - 1, Levels.Count) };
        if (_game.Pressed(Keys.Down)) _state = _state with { LevelIndex = MathUtil.Mod(_state.LevelIndex + 1, Levels.Count) };

        var available = Activities.Where(_state.Supports).ToArray();
        int activity = Math.Max(0, Array.IndexOf(available, _state.Activity));
        if (_game.Pressed(Keys.Left)) _state = _state with { Activity = available[Math.Max(0, activity - 1)] };
        if (_game.Pressed(Keys.Right)) _state = _state with { Activity = available[Math.Min(available.Length - 1, activity + 1)] };

        if (_game.Pressed(Keys.OemMinus) || _game.Pressed(Keys.Subtract))
            _state = _state with { BpmIndex = Math.Max(0, _state.BpmIndex - 1) };
        if (_game.Pressed(Keys.OemPlus) || _game.Pressed(Keys.Add))
            _state = _state with { BpmIndex = Math.Min(MenuState.BpmOptions.Length - 1, _state.BpmIndex + 1) };
        if (_game.Pressed(Keys.Tab))
        {
            int next = _state.Assist % MenuState.MaxAssist + 1;
            _state = _state with { Assist = Unlocks.IsAssistUnlocked(Progress, _state.At, next) ? next : 1 };
        }
        Normalize();

        if (_game.Pressed(Keys.A))
            _game.ShowScreen(new AudioSetupScreen(_game, _state));
        else if (_game.Pressed(Keys.T) || (_game.Clicked && TrainingPanel.Contains(_game.MousePosition)))
            _game.ShowScreen(new TrainingScreen(_game, _state with { Activity = Activity.Training }));
        else if (_game.Pressed(Keys.Enter) || _game.Pressed(Keys.Space))
        {
            if (Unlocks.IsLevelUnlocked(Progress, _state.At)) _game.ShowScreen(_state.CreateScreen(_game));
            else Notice(LockReason(_state.At));
        }
    }

    static string LockReason(LevelRef at)
    {
        if (at.Level > 0)
            return $"Bloqueado: faça {StarRating.ToPass}★ na Pista de \"{LevelFactory.Worlds[at.World].Levels[at.Level - 1].Title}\".";
        var previous = LevelFactory.Worlds[at.World - 1];
        return $"Bloqueado: passe o último nível do Mundo {at.World} ({previous.Levels[^1].Title}).";
    }

    static Rectangle ChipRect(int i) => new(60 + i * 200, 148, 188, 40);

    int StarsOf(int level)
    {
        var at = _state.At with { Level = level };
        return _state.Activity == Activity.Track
            ? Unlocks.TrackStars(Progress, at)
            : Progress.Stars((_state with { LevelIndex = level }).ProgressKey);
    }

    public void Draw(GameTime gameTime)
    {
        var p = _game.Primitives;
        _game.DrawBackground();
        for (int i = 0; i < Activities.Length; i++)
        {
            var chip = ChipRect(i);
            var color = Activities[i] == _state.Activity ? Palette.Gold : _state.Supports(Activities[i]) ? Palette.Panel : Palette.Panel * 0.5f;
            p.Rect(chip.X, chip.Y, chip.Width, chip.Height, color);
        }
        float rowY = ListY + (_state.LevelIndex - _scroll) * RowHeight;
        p.Rect(60, rowY, 560, RowHeight - 4, Palette.Panel);
        p.Rect(60, rowY, 5, RowHeight - 4, Palette.Gold);
        if (Levels.Count > VisibleRows)
        {
            float trackHeight = VisibleRows * RowHeight - 4;
            p.Rect(628, ListY, 4, trackHeight, Palette.Panel);
            p.Rect(628, ListY + trackHeight * _scroll / Levels.Count, 4, trackHeight * VisibleRows / Levels.Count, Palette.TextFaint);
        }
        int due = TrainingPlan.DueCount(Progress);
        p.Rect(TrainingPanel.X, TrainingPanel.Y, TrainingPanel.Width, TrainingPanel.Height,
            TrainingPanel.Contains(_game.MousePosition) ? Palette.Panel * 1.5f : Palette.Panel);
        p.Rect(TrainingPanel.X, TrainingPanel.Y, 5, TrainingPanel.Height, due > 0 ? Palette.Gold : Palette.Third);
        _fretboard.DrawBase(p);
        p.Flush();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

        _game.Text("GUITAR PLAY", new Vector2(60, 30), 54, Palette.Gold, bold: true, shadow: true);
        DrawWorldSelector();
        DrawInputStatus();

        for (int i = 0; i < Activities.Length; i++)
        {
            var chip = ChipRect(i);
            bool selected = Activities[i] == _state.Activity;
            var color = selected ? new Color(25, 20, 40) : _state.Supports(Activities[i]) ? Palette.TextDim : Palette.TextFaint * 0.6f;
            _game.Text($"{i + 1}. {MenuState.NameOf(Activities[i])}", new Vector2(chip.Center.X, chip.Center.Y), 17,
                color, bold: true, align: Align.Center, middle: true);
        }

        DrawLevelList();
        DrawActivityPanel();
        DrawTrainingPanel(due);

        var level = _state.Level;
        if (_time < _noticeUntil)
            _game.Text(_notice, new Vector2(40, 518), 18, Palette.Other, bold: true);
        else
            _game.Text($"{level.Title} · {level.Subtitle}", new Vector2(40, 518), 18, Palette.Text, bold: true);
        _fretboard.DrawSprites(_game, level.ShapeNotes, null, _time);

        _game.Batch.End();
    }

    void DrawWorldSelector()
    {
        var world = _state.World;
        bool unlocked = Unlocks.IsWorldUnlocked(Progress, _state.WorldIndex);
        var size = _game.Text($"◀  Mundo {_state.WorldIndex + 1}: {world.Title}  ▶", new Vector2(60, 100), 22,
            unlocked ? Palette.Text : Palette.TextFaint, bold: true);
        string detail = unlocked ? $"{world.Chapter} do livro   ·   Q / E troca de mundo" : $"{world.Chapter} · bloqueado   ·   Q / E troca de mundo";
        _game.Text(detail, new Vector2(80 + size.X, 105), 16, Palette.TextFaint);
    }

    void DrawLevelList()
    {
        for (int row = 0; row < VisibleRows && _scroll + row < Levels.Count; row++)
        {
            int i = _scroll + row;
            var level = Levels[i];
            float y = ListY + row * RowHeight;
            bool selected = i == _state.LevelIndex;
            bool open = Unlocks.IsLevelUnlocked(Progress, _state.At with { Level = i });
            var color = !open ? Palette.TextFaint : selected ? Palette.Text : Palette.TextDim;

            _game.Text($"{i + 1}", new Vector2(76, y + 10), 16, Palette.TextFaint);
            _game.Text(level.Title, new Vector2(104, y + 7), 20, color, bold: true);
            string detail = level.Subtitle.Replace("Sol ", "").Replace(" · sobe e desce", "");
            _game.Text(detail.Length > 34 ? detail[..33] + "…" : detail, new Vector2(300, y + 11), 15, Palette.TextFaint);

            if (!open)
                _game.Text("bloqueado", new Vector2(600, y + 11), 15, Palette.TextFaint, align: Align.Right);
            else
            {
                int stars = StarsOf(i);
                for (int s = 0; s < 3; s++)
                    _game.Text("★", new Vector2(530 + s * 24, y + 7), 22, s < stars ? Palette.Gold : Palette.Panel * 1.6f);
            }
        }
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
            if (next <= MenuState.MaxAssist && !Unlocks.IsAssistUnlocked(Progress, _state.At, next))
                _game.Text($"Ajuda {next} abre com {StarRating.ToPass}★ na Ajuda {_state.Assist}", new Vector2(x, 378), 15, Palette.TextFaint);
            help = new[] { "↑ ↓ nível  ·  ← → atividade", "+ − BPM  ·  Tab ajuda  ·  Enter começar" };
        }
        else
            help = new[] { "↑ ↓ nível  ·  ← → atividade", "Enter começar  ·  Esc sair" };
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
