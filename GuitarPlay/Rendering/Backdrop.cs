using Microsoft.Xna.Framework;

namespace GuitarPlay.Rendering;

/// <summary>
/// Fundo animado das telas fora da Pista: canhões de luz que varrem no ritmo e poeira de luz subindo.
/// Tudo gerado em código (a Pista usa o palco 2.5D de <see cref="Stage"/>).
/// </summary>
public sealed class Backdrop
{
    sealed record Beam(float X, float Swing, float Speed, float Phase, Color Color);

    sealed record Mote(float X, float Y, float Speed, float Size, float Phase);

    static readonly Color[] BeamColors =
    {
        new(170, 90, 255), new(60, 190, 255), new(255, 80, 170), new(255, 190, 70), new(90, 255, 200),
    };

    readonly Beam[] _beams;
    readonly Mote[] _motes;
    float _energy;

    public Backdrop(int seed)
    {
        var random = new Random(seed);
        float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);

        _beams = Enumerable.Range(0, 5).Select(i => new Beam(
            X: 80 + i * 280 + Next(-40, 40),
            Swing: Next(0.25f, 0.45f),
            Speed: Next(0.35f, 0.7f),
            Phase: Next(0, MathHelper.TwoPi),
            Color: BeamColors[i % BeamColors.Length])).ToArray();

        _motes = Enumerable.Range(0, 70).Select(_ => new Mote(
            Next(0, GuitarPlayGame.Width), Next(0, GuitarPlayGame.Height), Next(8, 30), Next(1.2f, 3f), Next(0, MathHelper.TwoPi))).ToArray();
    }

    public void Update(float dt, float targetEnergy) =>
        _energy += (Math.Clamp(targetEnergy, 0, 1) - _energy) * MathF.Min(1, dt * 1.5f);

    /// <param name="pulse">Brilho da batida: 1 no tempo e caindo até a próxima.</param>
    public void DrawBackdrop(Primitives p, double time, float pulse)
    {
        // Fundo que muda de tom devagar (roxo ↔ azul).
        float drift = 0.5f + 0.5f * MathF.Sin((float)time * 0.15f);
        var top = Color.Lerp(new Color(28, 16, 52), new Color(14, 26, 58), drift);
        p.VerticalGradient(0, 0, GuitarPlayGame.Width, GuitarPlayGame.Height, top, Palette.BackgroundBottom);

        float intensity = 0.07f + 0.08f * _energy + 0.12f * pulse * (0.4f + 0.6f * _energy);
        foreach (var beam in _beams)
        {
            float angle = MathF.Sin((float)time * beam.Speed + beam.Phase) * beam.Swing;
            var apex = new Vector2(beam.X, -30);
            var direction = new Vector2(MathF.Sin(angle), MathF.Cos(angle));
            var normal = new Vector2(-direction.Y, direction.X);
            var far = apex + direction * 820;
            var near = beam.Color * intensity;
            p.Quad(apex - normal * 8, apex + normal * 8, far + normal * 170, far - normal * 170,
                near, near, Color.Transparent, Color.Transparent);
        }

        foreach (var mote in _motes)
        {
            float y = MathUtilF.Wrap(mote.Y - (float)time * mote.Speed, GuitarPlayGame.Height);
            float x = mote.X + MathF.Sin((float)time * 0.5f + mote.Phase) * 12;
            float twinkle = 0.5f + 0.5f * MathF.Sin((float)time * 2 + mote.Phase * 3);
            p.Disc(new Vector2(x, y), mote.Size, Color.White * (0.05f + 0.1f * twinkle), 6);
        }
    }
}

static class MathUtilF
{
    /// <summary>Resto sempre positivo para floats (para posições que dão a volta na tela).</summary>
    public static float Wrap(float value, float range) => ((value % range) + range) % range;
}
