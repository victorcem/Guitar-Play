using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

/// <summary>
/// Mede o atraso total (som do clique + reação + captura + detecção): o jogador toca junto
/// com 5 cliques e a latência é a mediana dos desvios. A mediana ignora uma batida fora da curva.
/// </summary>
public sealed class CalibrationScreen : IScreen
{
    const int Bpm = 80;
    const int CountIn = 4;
    const int Beats = 5;
    const int MinimumHits = 4;
    // Janela em que uma nota conta para uma batida (antes / depois do clique).
    const double Before = 0.25, After = 0.45;
    // Desvio típico máximo entre as batidas para aceitar a medição.
    const double MaxDeviation = 0.04;

    enum Phase { Ready, Running, Done }

    readonly GuitarPlayGame _game;
    readonly MenuState _back;
    readonly double _secondsPerBeat = 60.0 / Bpm;
    readonly double?[] _offsets = new double?[Beats];
    Phase _phase = Phase.Ready;
    double _start;
    int _lastClick;
    float _flash;
    string _message = "";
    bool _succeeded;

    public CalibrationScreen(GuitarPlayGame game, MenuState back)
    {
        _game = game;
        _back = back;
    }

    double BeatTime(int beat) => _start + beat * _secondsPerBeat;
    int TotalBeats => CountIn + Beats;

    public void Update(GameTime gameTime)
    {
        _flash = MathF.Max(0, _flash - (float)gameTime.ElapsedGameTime.TotalSeconds * 4);
        if (_game.Pressed(Keys.Escape))
        {
            _game.ShowScreen(new AudioSetupScreen(_game, _back));
            return;
        }
        if (_phase != Phase.Running)
        {
            if (_game.Pressed(Keys.Enter) || _game.Pressed(Keys.Space)) Start();
            return;
        }

        double now = _game.Now;
        int beat = (int)Math.Floor((now - _start) / _secondsPerBeat);
        if (beat > _lastClick && beat < TotalBeats)
        {
            _lastClick = beat;
            _game.Metronome.Click(accent: beat >= CountIn);
            _flash = 1;
        }

        foreach (var (_, age) in _game.AudioNotes)
        {
            double played = now - age;
            for (int k = 0; k < Beats; k++)
            {
                double target = BeatTime(CountIn + k);
                if (_offsets[k] is not null || played < target - Before || played > target + After) continue;
                _offsets[k] = played - target;
                break;
            }
        }

        if (now > BeatTime(TotalBeats - 1) + After) Finish();
    }

    void Start()
    {
        Array.Clear(_offsets);
        _phase = Phase.Running;
        _start = _game.Now + 0.5;
        _lastClick = -1;
        _message = "";
    }

    void Finish()
    {
        _phase = Phase.Done;
        var values = _offsets.Where(o => o is not null).Select(o => o!.Value).ToList();
        if (values.Count < MinimumHits)
        {
            _succeeded = false;
            _message = $"Ouvi só {values.Count} de {Beats} batidas. Toque mais forte ou aumente a sensibilidade e tente de novo.";
            return;
        }

        double median = MathUtil.Median(values);
        double deviation = MathUtil.Median(values.Select(v => Math.Abs(v - median)));
        if (deviation > MaxDeviation)
        {
            _succeeded = false;
            _message = $"As batidas variaram muito (±{deviation * 1000:0} ms). Tente tocar bem em cima do clique.";
            return;
        }

        var settings = _game.Settings;
        settings.LatencySeconds = Math.Clamp(median, 0, 0.4);
        settings.Calibrated = true;
        settings.Save();
        _succeeded = true;
        _message = $"Latência calibrada: {settings.LatencySeconds * 1000:0} ms (variação ±{deviation * 1000:0} ms)";
    }

    public void Draw(GameTime gameTime)
    {
        _game.DrawBackground();
        _game.Primitives.Flush();
        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

        const float cx = GuitarPlayGame.Width / 2f;
        _game.Text("CALIBRAÇÃO DE LATÊNCIA", new Vector2(60, 28), 15, Palette.TextFaint, bold: true);
        _game.Text("Toque junto com o clique", new Vector2(60, 48), 34, Palette.Text, bold: true);
        _game.Text($"Toque uma corda solta exatamente em cada clique: {CountIn} tempos de contagem e depois {Beats} batidas.",
            new Vector2(60, 98), 17, Palette.TextDim);

        if (_game.Input is null)
        {
            _game.Text("Nenhuma entrada de áudio ativa. Volte (Esc) e escolha a Pocket Master ou a interface.",
                new Vector2(cx, 330), 20, Palette.Other, align: Align.Center);
            _game.Batch.End();
            return;
        }

        DrawPulse(cx);
        DrawSlots(cx);

        if (_message.Length > 0)
            _game.Text(_message, new Vector2(cx, 560), 20, _succeeded ? Palette.Third : Palette.Other, bold: true, align: Align.Center);
        Chrome.Footer(_game, _phase == Phase.Running ? "Esc cancelar"
            : _phase == Phase.Done ? "Enter repetir   ·   Esc voltar" : "Enter começar   ·   Esc voltar");
        _game.Batch.End();
    }

    void DrawPulse(float cx)
    {
        var center = new Vector2(cx, 290);
        _game.Circle(center, 90 + 14 * _flash, Palette.Panel);
        _game.Ring(center, 92 + 14 * _flash, Palette.Gold * (0.3f + 0.7f * _flash));

        string label;
        if (_phase == Phase.Ready) label = "Enter";
        else if (_phase == Phase.Done) label = _succeeded ? "OK" : "!";
        else
        {
            int beat = Math.Max(0, _lastClick);
            label = _lastClick < 0 ? "…" : beat < CountIn ? (CountIn - beat).ToString() : "Toque!";
        }
        _game.Text(label, center, label.Length > 2 ? 34 : 64, Palette.Text, bold: true, align: Align.Center, middle: true);
    }

    void DrawSlots(float cx)
    {
        for (int k = 0; k < Beats; k++)
        {
            var pos = new Vector2(cx + (k - (Beats - 1) / 2f) * 130, 450);
            bool current = _phase == Phase.Running && _lastClick == CountIn + k;
            var offset = _offsets[k];
            _game.Circle(pos, 30, offset is null ? Palette.Panel : Palette.Third);
            if (current) _game.Ring(pos, 36, Palette.Gold);
            _game.Text((k + 1).ToString(), pos, 22, offset is null ? Palette.TextFaint : new Color(15, 15, 25),
                bold: true, align: Align.Center, middle: true);
            if (offset is { } value)
                _game.Text($"{value * 1000:+0;-0;0} ms", pos + new Vector2(0, 44), 16, Palette.TextDim, align: Align.Center);
        }
    }
}
