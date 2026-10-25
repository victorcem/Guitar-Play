using System.Collections.Concurrent;
using GuitarPlay.Gameplay;

namespace GuitarPlay.Audio;

/// <summary>Uma nota tocada na guitarra. Timestamp no relógio do jogo (segundos).</summary>
public sealed record AudioNoteEvent(int Midi, double Timestamp, float Frequency);

/// <summary>O que o afinador mostra: frequência atual, clareza e volume.</summary>
public sealed record TunerReading(float Frequency, float Clarity, float Level);

/// <summary>
/// Transforma o áudio em notas: a cada ~10 ms mede o volume e a altura; quando uma nota
/// fica estável por 2 medições (e é nova ou foi palhetada de novo), gera um evento.
/// Roda na thread de captura de áudio.
/// </summary>
public sealed class NoteTracker
{
    const int TargetRate = 24000;
    const int Window = 1024;
    const int Hop = 256;
    const int StableFrames = 2;
    const float OnsetRatio = 1.6f;
    const float EnvelopeDecay = 0.9f;
    // Se houve palhetada até este tempo antes da nota estabilizar, o evento usa o instante da palhetada.
    const double OnsetLookback = 0.1;

    readonly Settings _settings;
    readonly int _decimation;
    readonly int _inputRate;
    readonly PitchDetector _detector;
    readonly float[] _window = new float[Window];
    readonly float[] _hop = new float[Hop];
    int _hopFill;
    float _pendingSum;
    int _pendingCount;

    float _envelope;
    bool _silent = true;
    int _candidate = -1;
    int _candidateFrames;
    double _candidateTime;
    int _lastEmitted = -1;
    bool _onsetSinceEmit;
    double _lastOnsetTime = double.NegativeInfinity;
    volatile TunerReading _reading = new(0, 0, 0);

    public NoteTracker(int inputRate, Settings settings)
    {
        _settings = settings;
        _inputRate = inputRate;
        // Reduz para ~24 kHz: sobra para a guitarra e deixa o YIN 4x mais leve.
        _decimation = Math.Max(1, inputRate / TargetRate);
        _detector = new PitchDetector(inputRate / _decimation, Window);
    }

    public ConcurrentQueue<AudioNoteEvent> Events { get; } = new();
    public TunerReading Reading => _reading;

    /// <param name="mono">Amostras recém-chegadas (mono, -1 a 1).</param>
    /// <param name="arrival">Quando o bloco chegou (relógio do jogo).</param>
    public void Process(ReadOnlySpan<float> mono, double arrival)
    {
        for (int i = 0; i < mono.Length; i++)
        {
            _pendingSum += mono[i];
            if (++_pendingCount < _decimation) continue;
            _hop[_hopFill++] = _pendingSum / _decimation;
            _pendingSum = 0;
            _pendingCount = 0;
            if (_hopFill < Hop) continue;

            _hopFill = 0;
            Array.Copy(_window, Hop, _window, 0, Window - Hop);
            Array.Copy(_hop, 0, _window, Window - Hop, Hop);
            Analyze(arrival - (mono.Length - 1 - i) / (double)_inputRate);
        }
    }

    void Analyze(double time)
    {
        float sum = 0;
        for (int i = 0; i < Hop; i++) sum += _hop[i] * _hop[i];
        float level = MathF.Sqrt(sum / Hop);

        bool onset = level > _settings.NoiseGate && level > _envelope * OnsetRatio;
        _envelope = MathF.Max(level, _envelope * EnvelopeDecay);

        if (level < _settings.NoiseGate)
        {
            _silent = true;
            _candidate = -1;
            _reading = new TunerReading(0, 0, level);
            return;
        }

        var (frequency, clarity) = _detector.Detect(_window);
        _reading = new TunerReading(frequency, clarity, level);

        // Palhetada nova: recomeça a contagem mesmo que a nota seja a mesma.
        if (onset)
        {
            _onsetSinceEmit = true;
            _lastOnsetTime = time;
            _candidate = -1;
        }
        if (frequency <= 0) return;

        int midi = (int)MathF.Round(69 + 12 * MathF.Log2(frequency / 440f));
        if (midi != _candidate)
        {
            _candidate = midi;
            _candidateFrames = 1;
            _candidateTime = time;
            return;
        }
        if (++_candidateFrames != StableFrames) return;

        if (midi != _lastEmitted || _onsetSinceEmit || _silent)
        {
            // O ataque é medido pelo volume (rápido e igual para toda nota); a altura demora mais
            // para estabilizar quando a nota muda. Usar o ataque deixa o tempo consistente.
            double sinceOnset = _candidateTime - _lastOnsetTime;
            double when = sinceOnset is >= 0 and <= OnsetLookback ? _lastOnsetTime : _candidateTime;
            Events.Enqueue(new AudioNoteEvent(midi, when, frequency));
            _lastEmitted = midi;
            _onsetSinceEmit = false;
            _silent = false;
        }
    }
}
