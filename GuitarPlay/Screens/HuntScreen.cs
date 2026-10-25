using GuitarPlay.Gameplay;
using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

/// <summary>
/// Quiz contra o relógio: só as tônicas aparecem; o jogador clica em todas as notas
/// do intervalo pedido (b3, depois 4, 5 e b7).
/// </summary>
public sealed class HuntScreen : IScreen
{
    const double TimeLimit = 60;
    const float FeedbackSeconds = 0.9f;

    readonly GuitarPlayGame _game;
    readonly MenuState _state;
    readonly Level _level;
    readonly IReadOnlyList<ScaleNote> _shape;
    readonly List<string> _targets;
    readonly HashSet<ScaleNote> _found = new();
    readonly FretboardView _board = new(new Rectangle(40, 300, 1200, 300));
    int _targetIndex;
    int _mistakes;
    double _time;
    double _clock;
    double _finishedAt = -1;
    (ScaleNote Note, float Age)? _wrong;

    public HuntScreen(GuitarPlayGame game, MenuState state)
    {
        _game = game;
        _state = state;
        _level = state.Level;
        _shape = _level.ShapeNotes;
        _targets = Pentatonic.Degrees(_level.Quality).Where(d => d != "1").ToList();
    }

    int Total => _shape.Count(n => n.Degree != "1");
    bool Finished => _finishedAt >= 0;
    string Target => _targets[Math.Min(_targetIndex, _targets.Count - 1)];
    int Remaining => _shape.Count(n => n.Degree == Target && !_found.Contains(n));

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _clock += dt;
        if (_game.Pressed(Keys.Escape))
        {
            _game.ShowScreen(new MenuScreen(_game, _state));
            return;
        }
        if (_wrong is { } wrong) _wrong = wrong.Age + dt > FeedbackSeconds ? null : (wrong.Note, wrong.Age + dt);

        if (Finished)
        {
            if (_clock - _finishedAt > 1.2) _game.ShowScreen(new ResultScreen(_game, BuildResult(), _state));
            return;
        }

        _time += dt;
        if (_time >= TimeLimit) Finish();
        else if (_game.Clicked && NoteAt(_game.MousePosition) is { } note) OnClick(note);
    }

    ScaleNote? NoteAt(Vector2 point)
    {
        if (_board.HitTest(point) is not { } hit) return null;
        foreach (var note in _shape)
            if (note.String == hit.String && note.Fret == hit.Fret) return note;
        return null;
    }

    void OnClick(ScaleNote note)
    {
        if (note.Degree == "1" || _found.Contains(note)) return;
        _game.Synth.Play(note.Midi);
        _game.Progress.RecordAnswer(_state.LevelIndex, Target, note.Degree == Target);
        if (note.Degree != Target)
        {
            _mistakes++;
            _wrong = (note, 0f);
            return;
        }

        _found.Add(note);
        if (Remaining > 0) return;
        _targetIndex++;
        if (_targetIndex >= _targets.Count) Finish();
    }

    void Finish() => _finishedAt = _clock;

    ActivityResult BuildResult()
    {
        int found = _found.Count;
        double precision = found + _mistakes == 0 ? 0 : found / (double)(found + _mistakes);
        double accuracy = found / (double)Total * precision;
        return new ActivityResult("Caça ao intervalo", $"{_level.Title} · {_level.KeyName}", accuracy, new[]
        {
            ("Encontradas", $"{found} de {Total}"),
            ("Cliques errados", _mistakes.ToString()),
            ("Tempo", $"{Math.Min(_time, TimeLimit):0} s"),
        }, _state.ProgressKey);
    }

    public void Draw(GameTime gameTime)
    {
        var p = _game.Primitives;
        _game.DrawBackground();
        Chrome.Bar(p, 60, 104, 1160, 1 - (float)(_time / TimeLimit), _time > TimeLimit - 10 ? Palette.Miss : Palette.Gold);
        _board.DrawBase(p);
        p.Flush();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        Chrome.Header(_game, "Caça ao intervalo", _level);
        _game.Text($"{Math.Max(0, TimeLimit - _time):0} s", new Vector2(1220, 56), 30, Palette.Text, bold: true, align: Align.Right);

        if (Finished)
            _game.Text(_found.Count == Total ? "Todas encontradas!" : "Tempo esgotado!", new Vector2(60, 140), 40, Palette.Gold, bold: true);
        else
        {
            var size = _game.Text("Ache todas as ", new Vector2(60, 140), 40, Palette.Text, bold: true);
            _game.Text($"{Target}  ({Pentatonic.DegreeName(Target)})", new Vector2(60 + size.X, 140), 40, Palette.Gold, bold: true);
            _game.Text(Remaining == 1 ? "falta 1" : $"faltam {Remaining}", new Vector2(60, 196), 20, Palette.TextDim);
        }

        for (int i = 0; i < _targets.Count; i++)
        {
            bool done = i < _targetIndex;
            var color = done ? Palette.ForDegree(_targets[i]) : i == _targetIndex && !Finished ? Palette.Text : Palette.TextFaint;
            _game.Text(done ? $"{_targets[i]} ✓" : _targets[i], new Vector2(60 + i * 90, 240), 22, color, bold: true);
        }
        _game.Text($"Erros: {_mistakes}", new Vector2(1220, 240), 20, _mistakes > 0 ? Palette.Miss : Palette.TextFaint, align: Align.Right);

        DrawBoard();
        Chrome.Footer(_game, "Clique nas notas com o mouse   ·   Esc menu");
        _game.Batch.End();
    }

    void DrawBoard()
    {
        _board.DrawInlays(_game);
        foreach (var note in _shape)
        {
            if (note.Degree == "1" || _found.Contains(note) || Finished)
                _board.DrawMark(_game, new FretMark(note.String, note.Fret, Palette.ForDegree(note.Degree), note.Degree));
            else
                _board.DrawMark(_game, new FretMark(note.String, note.Fret, new Color(105, 104, 130)));
        }

        if (_wrong is { } wrong)
        {
            float alpha = 1 - wrong.Age / FeedbackSeconds;
            var note = wrong.Note;
            _board.DrawMark(_game, new FretMark(note.String, note.Fret, Palette.ForDegree(note.Degree) * alpha, note.Degree));
            _game.Ring(_board.Position(note.String, note.Fret), _board.NoteRadius + 6, Palette.Miss * alpha);
            _game.Text($"era {note.Degree}", _board.Position(note.String, note.Fret) + new Vector2(_board.NoteRadius + 10, 0),
                20, Palette.Miss * alpha, bold: true, middle: true, shadow: true);
        }

        if (!Finished && NoteAt(_game.MousePosition) is { } hover && hover.Degree != "1" && !_found.Contains(hover))
            _game.Ring(_board.Position(hover.String, hover.Fret), _board.NoteRadius + 4, Color.White * 0.6f);
    }
}
