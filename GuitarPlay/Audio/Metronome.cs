using Microsoft.Xna.Framework.Audio;

namespace GuitarPlay.Audio;

/// <summary>Clique do metrônomo gerado em código (não precisa de arquivos de áudio).</summary>
public sealed class Metronome : IDisposable
{
    const int SampleRate = 44100;

    readonly SoundEffect _accent = MakeClick(1600f);
    readonly SoundEffect _tick = MakeClick(1000f);

    public void Click(bool accent) => (accent ? _accent : _tick).Play();

    static SoundEffect MakeClick(float frequency)
    {
        int samples = SampleRate * 35 / 1000;
        var buffer = new byte[samples * 2];
        for (int i = 0; i < samples; i++)
        {
            double t = i / (double)SampleRate;
            double envelope = Math.Exp(-t * 120);
            short sample = (short)(Math.Sin(2 * Math.PI * frequency * t) * envelope * 0.6 * short.MaxValue);
            buffer[2 * i] = (byte)sample;
            buffer[2 * i + 1] = (byte)(sample >> 8);
        }
        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    public void Dispose()
    {
        _accent.Dispose();
        _tick.Dispose();
    }
}
