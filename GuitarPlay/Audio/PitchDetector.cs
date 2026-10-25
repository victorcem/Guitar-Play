namespace GuitarPlay.Audio;

/// <summary>
/// Detecta a frequência fundamental com o algoritmo YIN (de Cheveigné e Kawahara, 2002).
/// Faixa pensada para guitarra: de ~70 Hz (abaixo do Mi grave, 82 Hz) a ~1400 Hz.
/// </summary>
public sealed class PitchDetector
{
    const float Threshold = 0.12f;

    readonly int _sampleRate;
    readonly int _minTau;
    readonly int _maxTau;
    readonly float[] _difference;

    public PitchDetector(int sampleRate, int window)
    {
        _sampleRate = sampleRate;
        _minTau = sampleRate / 1400;
        _maxTau = Math.Min(window / 2, sampleRate / 70);
        _difference = new float[_maxTau + 2];
    }

    /// <returns>Frequência em Hz e clareza (0 a 1); frequência 0 quando não há nota clara.</returns>
    public (float Frequency, float Clarity) Detect(float[] samples)
    {
        int width = samples.Length - _maxTau - 1;
        var d = _difference;

        // Função diferença normalizada pela média acumulada.
        d[0] = 1;
        float running = 0;
        for (int tau = 1; tau <= _maxTau; tau++)
        {
            float sum = 0;
            for (int j = 0; j < width; j++)
            {
                float delta = samples[j] - samples[j + tau];
                sum += delta * delta;
            }
            running += sum;
            d[tau] = running > 0 ? sum * tau / running : 1;
        }

        // Primeiro vale abaixo do limiar (evita confundir com a oitava de baixo).
        int best = -1;
        for (int tau = _minTau; tau <= _maxTau; tau++)
        {
            if (d[tau] >= Threshold) continue;
            while (tau + 1 <= _maxTau && d[tau + 1] < d[tau]) tau++;
            best = tau;
            break;
        }
        if (best < 0) return (0, 0);

        // Interpolação parabólica para precisão abaixo de uma amostra (importante para o afinador).
        float refined = best;
        if (best > 1 && best < _maxTau)
        {
            float s0 = d[best - 1], s1 = d[best], s2 = d[best + 1];
            float denominator = s0 - 2 * s1 + s2;
            if (MathF.Abs(denominator) > 1e-9f) refined = best + (s0 - s2) / (2 * denominator);
        }
        return (_sampleRate / refined, 1 - d[best]);
    }
}
