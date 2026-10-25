using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

/// <summary>
/// Lição guiada: primeiro as tônicas, depois um intervalo por passo, sempre desenhado
/// como um caminho a partir da tônica mais próxima ("constelação"), com som.
/// </summary>
public sealed class ConstellationScreen : IScreen
{
    const string AllStep = "*";

    static readonly Dictionary<string, string> Character = new()
    {
        ["1"] = "Repouso, a \"casa\". Frases que terminam aqui soam resolvidas.",
        ["b3"] = "Define o som menor (triste, pesado). Âncora: Smoke on the Water começa com 1 → b3 → 4.",
        ["4"] = "Suspensa: pede para andar até a b3 ou até a 5.",
        ["5"] = "A nota mais estável depois da tônica. Âncora: o tema de Star Wars abre com 1 → 5.",
        ["b7"] = "Tensão com sabor de blues e rock, que puxa de volta para a tônica.",
        ["2"] = "Nota de passagem, leve e aberta.",
        ["3"] = "Define o som maior (alegre, brilhante).",
        ["6"] = "Doce, muito comum no country e no pop.",
    };

    readonly GuitarPlayGame _game;
    readonly MenuState _state;
    readonly Level _level;
    readonly IReadOnlyList<ScaleNote> _shape;
    readonly List<string> _steps;
    readonly FretboardView _board = new(new Rectangle(40, 330, 1200, 300));
    int _step;
    double _time;
    double _stepTime;

    public ConstellationScreen(GuitarPlayGame game, MenuState state)
    {
        _game = game;
        _state = state;
        _level = state.Level;
        _shape = _level.ShapeNotes;
        _steps = Pentatonic.Degrees(_level.Quality).Append(AllStep).ToList();
        EnterStep(0);
    }

    string Current => _steps[_step];

    void EnterStep(int step)
    {
        _step = step;
        _stepTime = 0;
        PlayExample();
    }

    void PlayExample()
    {
        if (Current == AllStep)
            _game.Synth.PlaySequence(0.2, _shape.Select(n => n.Midi).ToArray());
        else if (Current == "1")
            _game.Synth.PlaySequence(0.5, Roots().Select(n => n.Midi).ToArray());
        else
        {
            var note = NotesOf(Current).First();
            _game.Synth.PlaySequence(0.5, FretboardGeometry.NearestRoot(_shape, note).Midi, note.Midi);
        }
    }

    IEnumerable<ScaleNote> Roots() => _shape.Where(n => n.Degree == "1").OrderBy(n => n.Midi);
    IEnumerable<ScaleNote> NotesOf(string degree) => _shape.Where(n => n.Degree == degree).OrderBy(n => n.Midi);

    public void Update(GameTime gameTime)
    {
        double dt = gameTime.ElapsedGameTime.TotalSeconds;
        _time += dt;
        _stepTime += dt;

        if (_game.Pressed(Keys.Escape)) _game.ShowScreen(new MenuScreen(_game, _state));
        else if (_game.Pressed(Keys.R)) PlayExample();
        else if (_game.Pressed(Keys.Left) && _step > 0) EnterStep(_step - 1);
        else if (_game.Pressed(Keys.Right) || _game.Pressed(Keys.Enter) || _game.Pressed(Keys.Space))
        {
            if (_step < _steps.Count - 1) EnterStep(_step + 1);
            else _game.ShowScreen(new HuntScreen(_game, _state with { Activity = Activity.Hunt }));
        }
    }

    /// <summary>Pares (de, até) desenhados como linhas no passo atual.</summary>
    List<(ScaleNote From, ScaleNote To)> Paths()
    {
        if (Current == AllStep) return new();
        if (Current == "1")
        {
            var roots = Roots().ToList();
            return roots.Zip(roots.Skip(1)).ToList();
        }
        return NotesOf(Current).Select(n => (FretboardGeometry.NearestRoot(_shape, n), n)).ToList();
    }

    public void Draw(GameTime gameTime)
    {
        var p = _game.Primitives;
        _game.DrawBackground();
        _board.DrawBase(p);

        var paths = Paths();
        var lineColor = Current == "1" ? Color.White * 0.7f : Palette.ForDegree(Current) * 0.9f;
        for (int i = 0; i < paths.Count; i++)
        {
            float progress = Math.Clamp((float)_stepTime * 2.5f - i * 0.35f, 0, 1);
            var from = _board.Position(paths[i].From.String, paths[i].From.Fret);
            var to = _board.Position(paths[i].To.String, paths[i].To.Fret);
            p.Line(from, Vector2.Lerp(from, to, progress), 5, lineColor);
        }

        for (int i = 0; i < _steps.Count; i++)
            p.Rect(1220 - (_steps.Count - i) * 34, 40, 26, 6, i <= _step ? Palette.Gold : Palette.Panel);
        p.Flush();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        Chrome.Header(_game, "Constelações", _level);
        _game.Text($"Passo {_step + 1} de {_steps.Count}", new Vector2(1220, 56), 15, Palette.TextFaint, align: Align.Right);
        DrawExplanation(paths);
        DrawBoard();
        Chrome.Footer(_game, _step < _steps.Count - 1
            ? "→ próximo   ·   ← voltar   ·   R ouvir de novo   ·   Esc menu"
            : "Enter: ir para a Caça ao intervalo   ·   ← voltar   ·   R ouvir de novo");
        _game.Batch.End();
    }

    void DrawExplanation(List<(ScaleNote From, ScaleNote To)> paths)
    {
        float y = 112;
        if (Current == AllStep)
        {
            _game.Text("O desenho completo", new Vector2(60, y), 36, Palette.Gold, bold: true);
            string order = string.Join(" → ", Pentatonic.Degrees(_level.Quality));
            _game.Text($"Cada nota tem um papel: {order}. Você não decorou 12 bolinhas,", new Vector2(60, y + 56), 20, Palette.TextDim);
            _game.Text("e sim 4 caminhos a partir da tônica. Agora, teste a memória na Caça ao intervalo.", new Vector2(60, y + 84), 20, Palette.TextDim);
            return;
        }

        var color = Palette.ForDegree(Current);
        string heading = Current == "1" ? "1 · tônica: o seu ponto de referência" : $"{Current} · {Pentatonic.DegreeName(Current)}";
        _game.Text(heading, new Vector2(60, y), 36, color, bold: true);

        var lines = new List<string>();
        if (Current == "1")
        {
            lines.Add("Ache primeiro as tônicas. Todos os outros intervalos você localiza a partir delas.");
            if (paths.Count > 0) lines.Add($"Oitava (tônica → tônica): {FretboardGeometry.Describe(paths[0].From, paths[0].To)}.");
            lines.Add("Âncora: Over the Rainbow abre com um salto de oitava.");
        }
        else
        {
            foreach (var group in paths.GroupBy(pt => FretboardGeometry.Describe(pt.From, pt.To)))
            {
                bool crossesB = group.Any(pt => FretboardGeometry.CrossesBString(pt.From, pt.To));
                lines.Add($"• Da tônica: {group.Key}{(crossesB ? "  (atravessa a corda Si, que desloca 1 casa)" : "")}");
            }
            lines.Add(Character[Current]);
        }
        for (int i = 0; i < lines.Count; i++)
            _game.Text(lines[i], new Vector2(60, y + 56 + i * 30), 20, i == lines.Count - 1 ? Palette.TextFaint : Palette.TextDim);
    }

    void DrawBoard()
    {
        _board.DrawInlays(_game);
        int currentIndex = _steps.IndexOf(Current);
        foreach (var note in _shape)
        {
            int degreeIndex = _steps.IndexOf(note.Degree);
            var color = Palette.ForDegree(note.Degree);
            if (note.Degree == "1" || Current == AllStep || degreeIndex < currentIndex)
                _board.DrawMark(_game, new FretMark(note.String, note.Fret, color, note.Degree));
            else if (degreeIndex == currentIndex)
            {
                float grow = Math.Clamp((float)_stepTime * 2.5f, 0, 1);
                _board.DrawMark(_game, new FretMark(note.String, note.Fret, color, note.Degree), 0.4f + 0.6f * grow);
                _board.DrawPulse(_game, note.String, note.Fret, _time, color);
            }
            else
                _board.DrawMark(_game, new FretMark(note.String, note.Fret, Color.White * 0.15f), 0.45f);
        }
    }
}
