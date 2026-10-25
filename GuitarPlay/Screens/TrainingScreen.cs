using GuitarPlay.Gameplay;
using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

/// <summary>
/// Treino do dia (repetição espaçada): mistura "qual é essa nota?" e "ache a nota" com os
/// intervalos que estão vencidos, os novos e os que você mais erra, em todos os desenhos.
/// </summary>
public sealed class TrainingScreen : IScreen
{
    enum Kind { NameIt, Find }

    sealed record Question(TrainingItem Item, Kind Kind, ScaleNote Note);

    readonly GuitarPlayGame _game;
    readonly MenuState _state;
    readonly List<Question> _questions;
    readonly FretboardView _board = new(new Rectangle(40, 230, 1200, 300));
    readonly AnswerButtons _buttons = new(Pentatonic.Degrees(ScaleQuality.Minor).Where(d => d != "1"));
    int _index;
    int _correct;
    double _time;
    double _questionTime;
    double _feedbackTime;
    bool? _right;
    string? _answer;
    ScaleNote? _clicked;

    public TrainingScreen(GuitarPlayGame game, MenuState state)
    {
        _game = game;
        _state = state;
        var random = new Random();
        _questions = TrainingPlan.Build(game.Progress, random).Select((item, i) =>
        {
            var options = LevelFactory.World1[item.LevelIndex].ShapeNotes.Where(n => n.Degree == item.Degree).ToList();
            return new Question(item, i % 2 == 0 ? Kind.NameIt : Kind.Find, options[random.Next(options.Count)]);
        }).ToList();
        Ask();
    }

    Question Current => _questions[_index];
    Level CurrentLevel => LevelFactory.World1[Current.Item.LevelIndex];
    IReadOnlyList<ScaleNote> Shape => CurrentLevel.ShapeNotes;
    bool Answered => _right is not null;

    void Ask()
    {
        _right = null;
        _answer = null;
        _clicked = null;
        _questionTime = 0;
        PlayQuestion();
    }

    void PlayQuestion() =>
        _game.Synth.PlaySequence(0.5, FretboardGeometry.NearestRoot(Shape, Current.Note).Midi, Current.Note.Midi);

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

        if (Answered)
        {
            _feedbackTime += dt;
            if (_feedbackTime < (_right == true ? 0.9 : 1.8)) return;
            if (++_index >= _questions.Count) _game.ShowScreen(new ResultScreen(_game, BuildResult(), _state));
            else Ask();
            return;
        }

        _questionTime += dt;
        if (Current.Kind == Kind.NameIt)
        {
            if (_buttons.Poll(_game) is { } answer)
            {
                _answer = answer;
                Answer(answer == Current.Item.Degree);
            }
        }
        else if (_game.Clicked && NoteAt(_game.MousePosition) is { } note && note.Degree != "1")
        {
            _clicked = note;
            _game.Synth.Play(note.Midi);
            Answer(note.Degree == Current.Item.Degree);
        }
    }

    ScaleNote? NoteAt(Vector2 point)
    {
        if (_board.HitTest(point) is not { } hit) return null;
        foreach (var note in Shape)
            if (note.String == hit.String && note.Fret == hit.Fret) return note;
        return null;
    }

    void Answer(bool right)
    {
        _right = right;
        _feedbackTime = 0;
        if (right) _correct++;
        _game.Progress.RecordAnswer(Current.Item.LevelIndex, Current.Item.Degree, right);
    }

    ActivityResult BuildResult()
    {
        int total = TrainingPlan.AllItems().Count();
        return new ActivityResult("Treino do dia", "Repetição espaçada · todos os desenhos", _correct / (double)_questions.Count, new[]
        {
            ("Acertos", $"{_correct} de {_questions.Count}"),
            ("Ainda para revisar hoje", TrainingPlan.DueCount(_game.Progress).ToString()),
            ("Intervalos dominados", $"{TrainingPlan.MasteredCount(_game.Progress)} de {total}"),
        });
    }

    /// <summary>"Próxima revisão: ..." do item atual, depois de responder.</summary>
    string NextReviewText()
    {
        if (_game.Progress.Review(Current.Item.LevelIndex, Current.Item.Degree) is not { } review) return "";
        int days = (review.Due - DateTime.Today).Days;
        string when = days <= 0 ? "ainda hoje" : days == 1 ? "amanhã" : $"em {days} dias";
        return $"Caixa {review.Box} de {Progress.MaxBox} · próxima revisão {when}";
    }

    public void Draw(GameTime gameTime)
    {
        var p = _game.Primitives;
        _game.DrawBackground();
        Chrome.Bar(p, 60, 104, 1160, _index / (float)_questions.Count, Palette.Gold);
        _board.DrawBase(p);
        if (Current.Kind == Kind.NameIt) _buttons.DrawBase(_game, _answer, Current.Item.Degree);
        p.Flush();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        Chrome.Header(_game, "Treino do dia", CurrentLevel);
        _game.Text($"Pergunta {_index + 1} de {_questions.Count}", new Vector2(1220, 40), 18, Palette.TextDim, align: Align.Right);
        _game.Text($"Acertos {_correct}", new Vector2(1220, 64), 18, Palette.TextFaint, align: Align.Right);

        DrawPrompt();
        DrawBoard();
        if (Current.Kind == Kind.NameIt)
        {
            _buttons.DrawText(_game, _answer, Current.Item.Degree);
            Chrome.Footer(_game, "Teclas 1 a 4 ou clique   ·   R ouvir de novo   ·   Esc menu");
        }
        else
            Chrome.Footer(_game, "Clique na nota   ·   R ouvir de novo   ·   Esc menu");
        _game.Batch.End();
    }

    void DrawPrompt()
    {
        string degree = Current.Item.Degree;
        string name = $"{degree} · {Pentatonic.DegreeName(degree)}";
        string prompt;
        if (!Answered)
            prompt = Current.Kind == Kind.NameIt ? "Qual é o grau da nota que está piscando?" : $"Clique em uma {name}";
        else if (_right == true)
            prompt = $"Isso! {name}";
        else
            prompt = Current.Kind == Kind.NameIt ? $"Era {name}" : $"Essa era {_clicked!.Value.Degree}. As {degree} estão marcadas.";
        var color = !Answered ? Palette.Text : _right == true ? Palette.Third : Palette.Miss;
        _game.Text(prompt, new Vector2(60, 140), 34, color, bold: true);

        if (Answered)
            _game.Text(NextReviewText(), new Vector2(60, 186), 18, Palette.Gold);
        else if (_questionTime > 4)
        {
            string where = FretboardGeometry.Describe(FretboardGeometry.NearestRoot(Shape, Current.Note), Current.Note);
            _game.Text($"Dica: partindo da tônica mais próxima, ela fica {where}.", new Vector2(60, 186), 18, Palette.TextDim);
        }
    }

    void DrawBoard()
    {
        _board.DrawInlays(_game);
        string target = Current.Item.Degree;
        foreach (var note in Shape)
        {
            bool reveal = Answered && (note.Degree == target || note == _clicked);
            if (note.Degree == "1")
                _board.DrawMark(_game, new FretMark(note.String, note.Fret, Palette.Root, "1"));
            else if (Current.Kind == Kind.NameIt && note == Current.Note && !Answered)
            {
                _board.DrawMark(_game, new FretMark(note.String, note.Fret, Color.White, "?"));
                _board.DrawPulse(_game, note.String, note.Fret, _time);
            }
            else if (reveal && (Current.Kind == Kind.Find || note == Current.Note))
                _board.DrawMark(_game, new FretMark(note.String, note.Fret, Palette.ForDegree(note.Degree), note.Degree), 1.1f);
            else
                _board.DrawMark(_game, new FretMark(note.String, note.Fret, Palette.Neutral));
        }

        if (_clicked is { } clicked && _right == false)
            _game.Ring(_board.Position(clicked.String, clicked.Fret), _board.NoteRadius + 6, Palette.Miss);

        if (Current.Kind == Kind.Find && !Answered && NoteAt(_game.MousePosition) is { } hover && hover.Degree != "1")
            _game.Ring(_board.Position(hover.String, hover.Fret), _board.NoteRadius + 4, Color.White * 0.6f);
    }
}
