using Microsoft.Xna.Framework.Audio;

namespace GuitarPlay.Audio;

/// <summary>
/// Som de corda dedilhada gerado em código (Karplus-Strong), para ouvir cada nota e intervalo.
/// Cada nota é sintetizada uma vez e guardada.
/// </summary>
public sealed class NoteSynth : IDisposable
{
    const int SampleRate = 44100;
    const double Seconds = 1.6;

    readonly Dictionary<int, SoundEffect> _cache = new();
    readonly List<(double At, int Midi)> _scheduled = new();
    double _clock;

    public void Play(int midi, float volume = 0.8f) => Get(midi).Play(volume, 0f, 0f);

    /// <summary>Toca as notas em sequência (ex.: tônica e depois o intervalo).</summary>
    public void PlaySequence(double gapSeconds, params int[] midis)
    {
        _scheduled.Clear();
        for (int i = 0; i < midis.Length; i++) _scheduled.Add((_clock + i * gapSeconds, midis[i]));
    }

    /// <summary>Chamar todo quadro para disparar as notas agendadas.</summary>
    public void Update(double elapsedSeconds)
    {
        _clock += elapsedSeconds;
        for (int i = _scheduled.Count - 1; i >= 0; i--)
        {
            if (_scheduled[i].At > _clock) continue;
            Play(_scheduled[i].Midi);
            _scheduled.RemoveAt(i);
        }
    }

    SoundEffect Get(int midi)
    {
        if (_cache.TryGetValue(midi, out var cached)) return cached;

        double frequency = 440 * Math.Pow(2, (midi - 69) / 12.0);
        int period = Math.Max(2, (int)Math.Round(SampleRate / frequency));
        var random = new Random(midi);
        var line = new double[period];
        for (int i = 0; i < period; i++) line[i] = random.NextDouble() * 2 - 1;
        // Suaviza o ruído inicial para um ataque menos estridente.
        for (int i = 1; i < period; i++) line[i] = (line[i] + line[i - 1]) / 2;

        int samples = (int)(SampleRate * Seconds);
        int fadeStart = samples - SampleRate / 5;
        var buffer = new byte[samples * 2];
        for (int i = 0; i < samples; i++)
        {
            int index = i % period;
            double value = line[index];
            line[index] = 0.996 * 0.5 * (line[index] + line[(index + 1) % period]);
            double fade = i < fadeStart ? 1 : (samples - i) / (double)(samples - fadeStart);
            short sample = (short)(Math.Clamp(value * 0.7 * fade, -1, 1) * short.MaxValue);
            buffer[2 * i] = (byte)sample;
            buffer[2 * i + 1] = (byte)(sample >> 8);
        }

        var effect = new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
        _cache[midi] = effect;
        return effect;
    }

    public void Dispose()
    {
        foreach (var effect in _cache.Values) effect.Dispose();
        _cache.Clear();
    }
}
