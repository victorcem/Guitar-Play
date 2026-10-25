using GuitarPlay.Gameplay;
using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

public sealed class PlayScreen : IScreen
{
    const double LookAheadBeats = 4;
    const int CountInBeats = 4;
    const float GemRadius = 30f;
    // Quanto a pista continua abaixo da linha de batida (a perspectiva cresce rápido para t negativo).
    const float NearT = -0.035f;

    static readonly string[] StringNames = { "Mi", "Si", "Sol", "Ré", "Lá", "Mi" };
    // Índice = corda - 1. A fileira A..H segue a ordem da pista (corda 6 à esquerda).
    static readonly Keys[] NumberKeys = { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6 };
    static readonly Keys[] RowKeys = { Keys.H, Keys.G, Keys.F, Keys.D, Keys.S, Keys.A };
    static readonly string[] RowKeyNames = { "H", "G", "F", "D", "S", "A" };

    readonly GuitarPlayGame _game;
    readonly MenuState _state;
    readonly Level _level;
    readonly int _bpm;
    readonly double _secondsPerBeat;
    readonly double _lookAhead;
    readonly double _endTime;
    readonly double[] _noteTimes;
    readonly Judge _judge;
    readonly Highway _highway = new(centerX: 450, strikeY: 462, farY: 30, laneSpacing: 84);
    readonly FretboardView _fretboard = new(new Rectangle(40, 548, 1200, 160));
    readonly float[] _laneFlash = new float[6];
    readonly List<Popup> _popups = new();
    double _time;
    int _lastClickBeat;

    sealed class Popup
    {
        public required string Text { get; init; }
        public required Color Color { get; init; }
        public required int String { get; init; }
        public float Age { get; set; }
    }

    /// <summary>Nível de ajuda: 1 cor + grau, 2 só a cor, 3 só a tônica, 4 sem ajuda.</summary>
    int Assist => _state.Assist;

    Color HintColor(string degree) => Assist switch
    {
        <= 2 => Palette.ForDegree(degree, _level.Extras),
        3 when degree == "1" => Palette.Root,
        _ => Palette.Neutral,
    };

    public PlayScreen(GuitarPlayGame game, MenuState state)
    {
        _game = game;
        _state = state;
        _level = state.Level;
        _bpm = state.Bpm;
        _secondsPerBeat = 60.0 / _bpm;
        _lookAhead = LookAheadBeats * _secondsPerBeat;
        _noteTimes = _level.Notes.Select(n => n.Beat * _secondsPerBeat).ToArray();
        _judge = new Judge(_level.Notes, _noteTimes);
        _endTime = _noteTimes[^1] + 2 * _secondsPerBeat;
        _time = -CountInBeats * _secondsPerBeat - 0.4;
        _lastClickBeat = (int)Math.Floor(_time / _secondsPerBeat);
    }

    public void Update(GameTime gameTime)
    {
        if (_game.Pressed(Keys.Escape))
        {
            _game.ShowScreen(new MenuScreen(_game, _state));
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _time += dt;

        int beat = (int)Math.Floor(_time / _secondsPerBeat);
        if (beat > _lastClickBeat)
        {
            _lastClickBeat = beat;
            if (_time < _endTime) _game.Metronome.Click(accent: MathUtil.Mod(beat, 4) == 0);
        }

        for (int s = 1; s <= 6; s++)
            if (_game.Pressed(NumberKeys[s - 1]) || _game.Pressed(RowKeys[s - 1]))
                OnPlayed(new PlayedNote(s, null), _time, fromGuitar: false);

        // A guitarra chega atrasada (captura + detecção + reação): a calibração mede esse atraso.
        double latency = _game.Settings.LatencySeconds;
        foreach (var (midi, age) in _game.AudioNotes)
            OnPlayed(new PlayedNote(null, midi), _time - age - latency, fromGuitar: true);

        double sweepAt = _game.Input is null ? _time : _time - latency - 0.05;
        foreach (var miss in _judge.SweepMisses(sweepAt))
            AddPopup("Errou", Palette.Miss, _level.Notes[miss.NoteIndex].String);

        for (int i = 0; i < _laneFlash.Length; i++) _laneFlash[i] = Math.Max(0, _laneFlash[i] - dt * 6);
        foreach (var popup in _popups) popup.Age += dt;
        _popups.RemoveAll(popup => popup.Age > 0.8f);

        if (_time >= _endTime) _game.ShowScreen(new ResultScreen(_game, BuildResult(), _state));
    }

    void OnPlayed(PlayedNote played, double playedAt, bool fromGuitar)
    {
        if (played.String is { } pressed) _laneFlash[pressed - 1] = 1;
        var judgement = _judge.Hit(played, playedAt, penalizeStray: !fromGuitar);
        if (judgement is null) return;

        var note = _level.Notes[judgement.NoteIndex];
        _laneFlash[note.String - 1] = 1;
        string text = judgement.Grade == HitGrade.Perfect ? "Perfeito!" : "Bom";
        // Sem o rótulo na nota, o grau aparece depois do acerto para reforçar a memória.
        if (Assist >= 2) text += $" · {note.Degree}";
        if (note.Bend > 0) text += " · bend";
        AddPopup(text, judgement.Grade == HitGrade.Perfect ? Palette.Gold : Palette.Third, note.String);
    }

    ActivityResult BuildResult() =>
        // O que vai ser liberado é calculado antes de a tela de resultado gravar as estrelas.
        new($"Pista · Ajuda {Assist}", $"{_level.Title} · {_level.Subtitle} · {_bpm} BPM", _judge.Accuracy, new[]
        {
            ("Pontos", _judge.Score.ToString("N0")),
            ("Maior combo", _judge.MaxCombo.ToString()),
            ("Perfeito / Bom / Errou", $"{_judge.Perfects} / {_judge.Goods} / {_judge.Misses}"),
        }, _state.ProgressKey, Unlocks.NewlyUnlocked(_game.Progress, _state.At, Assist));

    void AddPopup(string text, Color color, int guitarString) =>
        _popups.Add(new Popup { Text = text, Color = color, String = guitarString });

    public void Draw(GameTime gameTime)
    {
        _game.DrawBackground();
        DrawHighwayBase(_game.Primitives);
        _fretboard.DrawBase(_game.Primitives);
        _game.Primitives.Flush();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        DrawTargets();
        DrawNotes();
        DrawPopups();
        DrawHud();
        DrawCountIn();
        DrawFretboard();
        _game.Batch.End();
    }

    void DrawHighwayBase(Primitives p)
    {
        var h = _highway;
        var near = new Color(36, 38, 64);
        var far = new Color(12, 12, 26);
        p.Quad(h.Project(-3, NearT), h.Project(3, NearT), h.Project(3, 1), h.Project(-3, 1), near, near, far, far);

        foreach (float side in new[] { -3f, 3f })
            p.Quad(h.Project(side - 0.06f, NearT), h.Project(side + 0.06f, NearT),
                h.Project(side + 0.06f, 1), h.Project(side - 0.06f, 1), Palette.Gold * 0.6f);

        int firstBeat = (int)Math.Ceiling((_time + NearT * _lookAhead) / _secondsPerBeat);
        int lastBeat = (int)Math.Floor((_time + _lookAhead) / _secondsPerBeat);
        for (int b = firstBeat; b <= lastBeat; b++)
        {
            float t = (float)((b * _secondsPerBeat - _time) / _lookAhead);
            bool measure = MathUtil.Mod(b, 4) == 0;
            float half = measure ? 0.007f : 0.003f;
            p.Quad(h.Project(-3, t - half), h.Project(3, t - half), h.Project(3, t + half), h.Project(-3, t + half),
                Color.White * (measure ? 0.35f : 0.12f));
        }

        for (int s = 1; s <= 6; s++)
        {
            float x = Highway.LaneX(s);
            float half = 0.012f + (s - 1) * 0.005f;
            var color = Palette.GuitarString * (0.35f + 0.65f * _laneFlash[s - 1]);
            p.Quad(h.Project(x - half, NearT), h.Project(x + half, NearT), h.Project(x + half, 1), h.Project(x - half, 1), color);
        }

        p.Quad(h.Project(-3, -0.004f), h.Project(3, -0.004f), h.Project(3, 0.004f), h.Project(-3, 0.004f), Color.White * 0.5f);
    }

    void DrawTargets()
    {
        for (int s = 1; s <= 6; s++)
        {
            var pos = _highway.Project(Highway.LaneX(s), 0);
            float flash = _laneFlash[s - 1];
            _game.Circle(pos, GemRadius + 3, Color.Black * 0.55f);
            _game.Ring(pos, GemRadius + 2, Palette.GuitarString * (0.5f + 0.5f * flash));
            if (flash > 0) _game.Circle(pos, GemRadius - 2, Color.White * (0.45f * flash));
            _game.Text($"{s}ª {StringNames[s - 1]}", pos + new Vector2(0, GemRadius + 6), 15, Palette.TextDim, align: Align.Center);
            _game.Text($"{s} · {RowKeyNames[s - 1]}", pos + new Vector2(0, GemRadius + 24), 12, Palette.TextFaint, align: Align.Center);
        }
    }

    void DrawNotes()
    {
        // De trás para frente: as notas mais próximas ficam por cima.
        for (int i = _level.Notes.Count - 1; i >= 0; i--)
        {
            var grade = _judge.GradeOf(i);
            if (grade is HitGrade.Perfect or HitGrade.Good) continue;

            float t = (float)((_noteTimes[i] - _time) / _lookAhead);
            if (t > 1.02f || t < NearT) continue;

            var note = _level.Notes[i];
            float scale = Highway.Scale(t);
            float radius = GemRadius * scale;
            var pos = _highway.Project(Highway.LaneX(note.String), t);
            float alpha = t > 0.85f ? (1.02f - t) / 0.17f : 1f;
            var color = grade == HitGrade.Miss ? Palette.Missed : HintColor(note.Degree);

            _game.Circle(pos + new Vector2(0, radius * 0.15f), radius * 1.1f, Color.Black * (0.6f * alpha));
            _game.Circle(pos, radius, color * alpha);
            _game.Circle(pos - new Vector2(radius * 0.25f, radius * 0.3f), radius * 0.35f, Color.White * (0.25f * alpha));
            _game.Text(note.Fret.ToString(), pos, MathF.Max(10, 28 * scale), Color.White * alpha,
                bold: true, align: Align.Center, middle: true, shadow: true);
            if (Assist == 1 && scale > 0.5f)
                _game.Text(note.Degree, pos + new Vector2(radius + 4, -radius), 15 * scale, color * alpha, bold: true);
            if (note.Bend > 0)
            {
                // Bend: anel dourado e a amplitude acima da nota ("↑ 1 tom").
                _game.Ring(pos, radius * 1.25f, Palette.Gold * alpha);
                _game.Text($"↑ {BendLabel(note.Bend)}", pos - new Vector2(0, radius * 1.3f + 18 * scale), MathF.Max(11, 18 * scale),
                    Palette.Gold * alpha, bold: true, align: Align.Center, shadow: true);
            }
        }
    }

    static string BendLabel(int semitones) => semitones switch
    {
        1 => "½ tom",
        2 => "1 tom",
        3 => "1½ tom",
        _ => $"{semitones / 2.0:0.#} tons",
    };

    void DrawFretboard()
    {
        _fretboard.DrawInlays(_game);
        foreach (var note in _level.ShapeNotes)
        {
            string? label = Assist == 1 || (Assist == 3 && note.Degree == "1") ? note.Degree : null;
            _fretboard.DrawMark(_game, new FretMark(note.String, note.Fret, HintColor(note.Degree), label));
        }
        int next = _judge.NextPending;
        if (next >= 0) _fretboard.DrawPulse(_game, _level.Notes[next].String, _level.Notes[next].Fret, _time);
    }

    void DrawPopups()
    {
        foreach (var popup in _popups)
        {
            var pos = _highway.Project(Highway.LaneX(popup.String), 0.08f) - new Vector2(0, popup.Age * 40);
            float alpha = 1 - popup.Age / 0.8f;
            _game.Text(popup.Text, pos, 20, popup.Color * alpha, bold: true, align: Align.Center, shadow: true);
        }
    }

    void DrawHud()
    {
        const float x = 790;
        _game.Text(_level.Title, new Vector2(x, 30), 40, Palette.Text, bold: true);
        _game.Text($"{_level.Subtitle} · {_bpm} BPM", new Vector2(x, 82), 18, Palette.TextDim);
        _game.Text($"Ajuda {Assist}/{MenuState.MaxAssist} · {MenuState.AssistName(Assist)}", new Vector2(x, 104), 16, Palette.Gold, bold: true);

        _game.Text("PONTOS", new Vector2(x, 130), 14, Palette.TextFaint, bold: true);
        _game.Text(_judge.Score.ToString("N0"), new Vector2(x, 148), 44, Palette.Text, bold: true);

        _game.Text("COMBO", new Vector2(x, 214), 14, Palette.TextFaint, bold: true);
        _game.Text(_judge.Combo.ToString(), new Vector2(x, 232), 36, Palette.Text, bold: true);
        _game.Text($"x{_judge.Multiplier}", new Vector2(x + 110, 232), 36, Palette.Gold, bold: true);

        _game.Text("PRECISÃO", new Vector2(x + 220, 214), 14, Palette.TextFaint, bold: true);
        _game.Text($"{_judge.RunningAccuracy:P0}", new Vector2(x + 220, 232), 36, Palette.Text, bold: true);

        float progress = (float)Math.Clamp(_time / _endTime, 0, 1);
        DrawBar(x, 300, 440, progress);

        // Legenda montada com os graus que aparecem no nível (menor, maior ou os dois).
        var degrees = _level.Notes.Select(n => n.Degree).Distinct().ToList();
        var thirds = degrees.Where(d => d is "b3" or "3");
        var extras = degrees.Where(d => _level.Extras?.Contains(d) == true).ToList();
        var others = degrees.Where(d => d is not ("1" or "b3" or "3" or "5" or "#4") && !extras.Contains(d)).OrderBy(d => d.TrimStart('b', '#'));
        var legend = new List<(string Label, Color Color)>
        {
            ("Tônica (1)", Palette.Root),
            ($"Terça ({string.Join(" / ", thirds)})", Palette.Third),
            ("Quinta (5)", Palette.Fifth),
        };
        if (degrees.Contains("#4")) legend.Add(("Blue note (#4)", Palette.BlueNote));
        if (extras.Count > 0) legend.Add(($"Notas novas ({string.Join(", ", extras)})", Palette.Added));
        legend.Add(($"Outras ({string.Join(", ", others)})", Palette.Other));
        string[] labels = legend.Select(l => l.Label).ToArray();
        Color[] colors = legend.Select(l => l.Color).ToArray();
        int legendLines = Assist <= 2 ? labels.Length : Assist == 3 ? 1 : 0;
        if (Assist >= 3)
            _game.Text("O grau aparece só depois do acerto.", new Vector2(x, 340 + legendLines * 26), 16, Palette.TextFaint);
        for (int i = 0; i < legendLines; i++)
        {
            float y = 340 + i * 26;
            _game.Circle(new Vector2(x + 9, y + 11), 8, colors[i]);
            _game.Text(labels[i], new Vector2(x + 26, y), 17, Palette.TextDim);
        }
        string input = _game.Input is null ? "teclado" : "guitarra";
        _game.Text($"Entrada: {input}   ·   Esc: voltar ao menu", new Vector2(x, 470), 14, Palette.TextFaint);
    }

    void DrawBar(float x, float y, float width, float fraction)
    {
        // Barra feita com discos para não precisar de outra textura.
        for (float px = 0; px <= width; px += 3)
            _game.Circle(new Vector2(x + px, y), 3, px / width <= fraction ? Palette.Gold : Palette.Panel);
    }

    void DrawCountIn()
    {
        if (_time >= 0) return;
        int remaining = (int)Math.Ceiling(-_time / _secondsPerBeat);
        if (remaining is < 1 or > CountInBeats) return;
        _game.Text(remaining.ToString(), new Vector2(_highway.CenterX, 220), 120, Color.White * 0.9f,
            bold: true, align: Align.Center, middle: true, shadow: true);
    }
}
