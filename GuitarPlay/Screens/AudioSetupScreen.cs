using GuitarPlay.Audio;
using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

/// <summary>Escolha da entrada (Pocket Master, interface, microfone ou teclado) com afinador e medidor de volume.</summary>
public sealed class AudioSetupScreen : IScreen
{
    const float ListX = 60, ListY = 150, RowHeight = 46;
    static readonly int[] OpenStrings = { 64, 59, 55, 50, 45, 40 };
    static readonly string[] StringNames = { "Mi", "Si", "Sol", "Ré", "Lá", "Mi" };

    readonly GuitarPlayGame _game;
    readonly MenuState _back;
    readonly List<AudioInput.DeviceInfo?> _options;
    readonly List<string> _recent = new();
    int _selected;
    float _smoothedCents;
    double _time;

    public AudioSetupScreen(GuitarPlayGame game, MenuState back)
    {
        _game = game;
        _back = back;
        // Primeira opção é o teclado (sem áudio).
        _options = new List<AudioInput.DeviceInfo?> { null };
        try
        {
            _options.AddRange(AudioInput.ListDevices().Select(d => (AudioInput.DeviceInfo?)d));
        }
        catch (Exception e) when (e is System.Runtime.InteropServices.COMException or UnauthorizedAccessException)
        {
            // Sem acesso ao áudio do Windows: fica só a opção do teclado.
        }
        _selected = Math.Max(0, _options.FindIndex(o => o?.Id == game.Settings.InputDeviceId));
    }

    public void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        if (_game.Pressed(Keys.Escape)) _game.ShowScreen(new MenuScreen(_game, _back));
        if (_game.Pressed(Keys.Up)) _selected = MathUtil.Mod(_selected - 1, _options.Count);
        if (_game.Pressed(Keys.Down)) _selected = MathUtil.Mod(_selected + 1, _options.Count);
        if (_game.Pressed(Keys.Enter)) _game.UseInput(_options[_selected]?.Id);
        if (_game.Pressed(Keys.C) && _game.Input is not null) _game.ShowScreen(new CalibrationScreen(_game, _back));

        var settings = _game.Settings;
        if (_game.Pressed(Keys.OemPlus) || _game.Pressed(Keys.Add))
        {
            settings.NoiseGate = MathF.Max(0.001f, settings.NoiseGate / 1.4f);
            settings.Save();
        }
        if (_game.Pressed(Keys.OemMinus) || _game.Pressed(Keys.Subtract))
        {
            settings.NoiseGate = MathF.Min(0.2f, settings.NoiseGate * 1.4f);
            settings.Save();
        }

        foreach (var (midi, _) in _game.AudioNotes)
        {
            _recent.Insert(0, NoteName(midi));
            if (_recent.Count > 8) _recent.RemoveAt(_recent.Count - 1);
        }
    }

    static string NoteName(int midi) => $"{NoteNames.Of(midi)}{midi / 12 - 1}";

    public void Draw(GameTime gameTime)
    {
        var p = _game.Primitives;
        _game.DrawBackground();

        p.Rect(ListX, ListY + _selected * RowHeight, 520, RowHeight - 6, Palette.Panel);
        p.Rect(ListX, ListY + _selected * RowHeight, 5, RowHeight - 6, Palette.Gold);

        var reading = _game.Input?.Reading;
        DrawLevelMeter(p, reading?.Level ?? 0);
        DrawTunerBase(p, reading);
        p.Flush();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        _game.Text("ENTRADA DE ÁUDIO", new Vector2(60, 28), 15, Palette.TextFaint, bold: true);
        _game.Text("Conecte a guitarra", new Vector2(60, 48), 34, Palette.Text, bold: true);
        _game.Text("Pocket Master pelo USB, interface de áudio ou microfone. Use um timbre limpo, sem distorção nem delay.",
            new Vector2(60, 98), 17, Palette.TextDim);

        DrawDeviceList();
        DrawTunerText(reading);
        DrawStatus();
        Chrome.Footer(_game, "↑ ↓ escolher   ·   Enter usar   ·   C calibrar latência   ·   + − sensibilidade   ·   Esc voltar");
        _game.Batch.End();
    }

    void DrawDeviceList()
    {
        for (int i = 0; i < _options.Count; i++)
        {
            var option = _options[i];
            bool active = option?.Id == _game.Settings.InputDeviceId && (option is null || _game.Input is not null);
            string name = option?.Name ?? "Teclado (sem áudio)";
            float y = ListY + i * RowHeight;
            _game.Text(active ? "●" : "○", new Vector2(ListX + 18, y + 8), 18, active ? Palette.Third : Palette.TextFaint);
            _game.Text(name.Length > 44 ? name[..43] + "…" : name, new Vector2(ListX + 46, y + 8), 19,
                i == _selected ? Palette.Text : Palette.TextDim, bold: i == _selected);
        }
        if (_options.Count == 1)
            _game.Text("Nenhum dispositivo de entrada encontrado. Conecte a Pocket Master e volte aqui.",
                new Vector2(ListX, ListY + RowHeight + 10), 16, Palette.Other);
    }

    void DrawStatus()
    {
        var settings = _game.Settings;
        float y = 470;
        if (_game.InputError is not null)
            _game.Text($"Não foi possível abrir o dispositivo: {_game.InputError}", new Vector2(60, y), 16, Palette.Miss);
        else if (_game.Input is not null)
        {
            string latency = settings.Calibrated
                ? $"Latência calibrada: {settings.LatencySeconds * 1000:0} ms"
                : "Latência ainda não calibrada (tecla C)";
            _game.Text($"Usando: {_game.Input.DeviceName} · {_game.Input.SampleRate / 1000.0:0.#} kHz", new Vector2(60, y), 17, Palette.Text, bold: true);
            _game.Text(latency, new Vector2(60, y + 26), 17, settings.Calibrated ? Palette.Third : Palette.Other);
        }
        else
            _game.Text("Usando o teclado. Escolha um dispositivo e aperte Enter para tocar com a guitarra.", new Vector2(60, y), 17, Palette.TextDim);

        _game.Text("NOTAS DETECTADAS", new Vector2(60, 540), 14, Palette.TextFaint, bold: true);
        _game.Text(_recent.Count == 0 ? "Toque uma nota…" : string.Join("   ", _recent), new Vector2(60, 560), 22,
            _recent.Count == 0 ? Palette.TextFaint : Palette.Text, bold: true);
    }

    // Medidor de volume: barra em escala de dB (-60 a 0) com a marca da sensibilidade (noise gate).
    void DrawLevelMeter(Primitives p, float level)
    {
        const float x = 680, y = 520, width = 540;
        static float ToFraction(float rms) => Math.Clamp((20 * MathF.Log10(MathF.Max(rms, 1e-6f)) + 60) / 60, 0, 1);
        bool aboveGate = level >= _game.Settings.NoiseGate;
        p.Rect(x, y, width, 14, Palette.Panel);
        p.Rect(x, y, width * ToFraction(level), 14, aboveGate ? Palette.Third : Palette.TextFaint);
        p.Rect(x + width * ToFraction(_game.Settings.NoiseGate) - 1, y - 4, 3, 22, Palette.Gold);
    }

    void DrawTunerBase(Primitives p, TunerReading? reading)
    {
        const float cx = 950, y = 330, half = 260;
        p.Rect(cx - half, y, half * 2, 4, Palette.Panel);
        for (int c = -50; c <= 50; c += 10)
            p.Rect(cx + c / 50f * half - 1, y - (c == 0 ? 18 : 8), 2, c == 0 ? 40 : 20, c == 0 ? Palette.Third : Palette.TextFaint);

        if (reading is not { Frequency: > 0 } r) return;
        float cents = Cents(r.Frequency);
        _smoothedCents += (cents - _smoothedCents) * 0.3f;
        var color = MathF.Abs(_smoothedCents) <= 5 ? Palette.Third : MathF.Abs(_smoothedCents) <= 20 ? Palette.Gold : Palette.Miss;
        float nx = cx + Math.Clamp(_smoothedCents, -50, 50) / 50f * half;
        p.Rect(nx - 3, y - 34, 6, 72, color);
    }

    static float Cents(float frequency)
    {
        float midi = 69 + 12 * MathF.Log2(frequency / 440f);
        return (midi - MathF.Round(midi)) * 100;
    }

    void DrawTunerText(TunerReading? reading)
    {
        const float cx = 950;
        _game.Text("AFINADOR", new Vector2(680, 150), 15, Palette.TextFaint, bold: true);
        if (_game.Input is null)
        {
            _game.Text("Escolha um dispositivo para ver o afinador.", new Vector2(cx, 230), 18, Palette.TextFaint, align: Align.Center);
            return;
        }
        if (reading is not { Frequency: > 0 } r)
        {
            _game.Text("—", new Vector2(cx, 230), 64, Palette.TextFaint, bold: true, align: Align.Center, middle: true);
            _game.Text("toque uma corda solta", new Vector2(cx, 390), 18, Palette.TextFaint, align: Align.Center);
        }
        else
        {
            int midi = (int)MathF.Round(69 + 12 * MathF.Log2(r.Frequency / 440f));
            var color = MathF.Abs(_smoothedCents) <= 5 ? Palette.Third : Palette.Text;
            _game.Text(NoteName(midi), new Vector2(cx, 230), 72, color, bold: true, align: Align.Center, middle: true);
            _game.Text($"{r.Frequency:0.0} Hz   ·   {_smoothedCents:+0;-0;0} cents", new Vector2(cx, 390), 18, Palette.TextDim, align: Align.Center);
            int index = Array.FindIndex(OpenStrings, s => Math.Abs(s - midi) <= 2);
            if (index >= 0)
                _game.Text($"perto da {index + 1}ª corda ({StringNames[index]} solta)", new Vector2(cx, 418), 16, Palette.TextFaint, align: Align.Center);
        }
        _game.Text("VOLUME", new Vector2(680, 496), 14, Palette.TextFaint, bold: true);
        _game.Text("linha dourada = sensibilidade", new Vector2(1220, 496), 14, Palette.TextFaint, align: Align.Right);
    }
}
