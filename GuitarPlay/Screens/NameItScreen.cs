using GuitarPlay.Gameplay;
using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

/// <summary>
/// Quiz de reconhecimento: uma nota do desenho acende e toca depois da tônica;
/// o jogador responde qual é o grau (teclas 1 a 4 ou clique).
/// </summary>
public sealed class NameItScreen : IScreen
{
    const int Questions = 12;

    readonly GuitarPlayGame _game;
    readonly MenuState _state;
    readonly Level _level;
    readonly IReadOnlyList<ScaleNote> _shape;
    readonly AnswerButtons _buttons;
    readonly List<ScaleNote> _queue;
    readonly List<double> _answerTimes = new();
    readonly FretboardView _board = new(new Rectangle(40, 230, 1200, 300));
    int _index;
    int _correct;
    int _streak;
    int _bestStreak;
    double _time;
    double _questionTime;
    double _feedbackTime;
    string? _answer;

    public NameItScreen(GuitarPlayGame game, MenuState state)
    {
        _game = game;
        _state = state;
        _level = state.Level;
        _shape = _level.ShapeNotes;
        _buttons = new AnswerButtons(Pentatonic.Degrees(_level.Quality).Where(d => d != "1"));
        _queue = BuildQueue(_shape.Where(n => n.Degree != "1").ToList());
        Ask();
    }

    ScaleNote Current => _queue[_index];
    bool WasRight => _answer == Current.Degree;

    /// <summary>Todas as notas (fora as tônicas) embaralhadas, completando com sorteios sem repetir a anterior.</summary>
    static List<ScaleNote> BuildQueue(List<ScaleNote> notes)
    {
        var random = new Random();
        var queue = notes.OrderBy(_ => random.Next()).ToList();
        while (queue.Count < Questions)
        {
            var pick = notes[random.Next(notes.Count)];
            if (pick != queue[^1]) queue.Add(pick);
        }
        return queue.Take(Questions).ToList();
    }

    void Ask()
    {
        _answer = null;
        _questionTime = 0;
        PlayQuestion();
    }

    void PlayQuestion() =>
        _game.Synth.PlaySequence(0.5, FretboardGeometry.NearestRoot(_shape, Current).Midi, Current.Midi);

    public void Update(GameTime gameTime)
    {
        double dt = gameTime.ElapsedGameTime.TotalSeconds;
        _time += dt;
        if (_game.Pressed(Keys.Escape))
        {
            _game.ShowScreen(new MenuScreen(_game, _state));
            return;
        }
        if (_game.Pressed(Keys.R)) PlayQuestion();

        if (_answer is not null)
        {
            _feedbackTime += dt;
            if (_feedbackTime < (WasRight ? 0.7 : 1.5)) return;
            if (++_index >= _queue.Count) _game.ShowScreen(new ResultScreen(_game, BuildResult(), _state));
            else Ask();
            return;
        }

        _questionTime += dt;
        if (_buttons.Poll(_game) is { } answer) Answer(answer);
    }

    void Answer(string degree)
    {
        _answer = degree;
        _feedbackTime = 0;
        _game.Progress.RecordAnswer(_state.At, Current.Degree, WasRight);
        if (WasRight)
        {
            _correct++;
            _streak++;
            _bestStreak = Math.Max(_bestStreak, _streak);
            _answerTimes.Add(_questionTime);
        }
        else _streak = 0;
    }

    ActivityResult BuildResult()
    {
        string average = _answerTimes.Count == 0 ? "-" : $"{_answerTimes.Average():0.0} s";
        return new ActivityResult("Qual é essa nota?", $"{_level.Title} · {_level.KeyName}", _correct / (double)Questions, new[]
        {
            ("Acertos", $"{_correct} de {Questions}"),
            ("Tempo médio (acertos)", average),
            ("Maior sequência", _bestStreak.ToString()),
        }, _state.ProgressKey);
    }

    public void Draw(GameTime gameTime)
    {
        var p = _game.Primitives;
        _game.DrawBackground();
        Chrome.Bar(p, 60, 104, 1160, _index / (float)Questions, Palette.Gold);
        _board.DrawBase(p);
        _buttons.DrawBase(_game, _answer, Current.Degree);
        p.Flush();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        Chrome.Header(_game, "Qual é essa nota?", _level);
        _game.Text($"Pergunta {Math.Min(_index + 1, Questions)} de {Questions}", new Vector2(1220, 40), 18, Palette.TextDim, align: Align.Right);
        _game.Text($"Acertos {_correct}   ·   Sequência {_streak}", new Vector2(1220, 64), 18,
            _streak >= 3 ? Palette.Gold : Palette.TextFaint, align: Align.Right);

        string prompt = _answer is null ? "Qual é o grau da nota que está piscando?"
            : WasRight ? $"Isso! {Current.Degree} · {Pentatonic.DegreeName(Current.Degree)}"
            : $"Era {Current.Degree} · {Pentatonic.DegreeName(Current.Degree)}";
        var promptColor = _answer is null ? Palette.Text : WasRight ? Palette.Third : Palette.Miss;
        _game.Text(prompt, new Vector2(60, 140), 34, promptColor, bold: true);
        // A dica geográfica só aparece se a resposta demorar (ou depois de errar).
        if ((_answer is null && _questionTime > 4) || (_answer is not null && !WasRight))
        {
            string where = FretboardGeometry.Describe(FretboardGeometry.NearestRoot(_shape, Current), Current);
            _game.Text($"Dica: partindo da tônica mais próxima, ela fica {where}.", new Vector2(60, 186), 18, Palette.TextDim);
        }

        DrawBoard();
        _buttons.DrawText(_game, _answer, Current.Degree);
        Chrome.Footer(_game, "Teclas 1 a 4 ou clique   ·   R ouvir de novo   ·   Esc menu");
        _game.Batch.End();
    }

    void DrawBoard()
    {
        _board.DrawInlays(_game);
        foreach (var note in _shape)
        {
            if (note == Current) continue;
            _board.DrawMark(_game, note.Degree == "1"
                ? new FretMark(note.String, note.Fret, Palette.Root, "1")
                : new FretMark(note.String, note.Fret, new Color(105, 104, 130)));
        }

        if (_answer is null)
        {
            _board.DrawMark(_game, new FretMark(Current.String, Current.Fret, Color.White, "?"));
            _board.DrawPulse(_game, Current.String, Current.Fret, _time);
        }
        else
            _board.DrawMark(_game, new FretMark(Current.String, Current.Fret, Palette.ForDegree(Current.Degree), Current.Degree), 1.15f);
    }
}
