using Microsoft.Xna.Framework;

namespace GuitarPlay.Rendering;

/// <summary>
/// Palco de show ao fundo: canhões de luz que varrem o palco no ritmo, poeira de luz subindo
/// e uma plateia em silhueta que pula na batida e se anima com o combo. Tudo gerado em código.
/// </summary>
public sealed class Stage
{
    sealed record Beam(float X, float Swing, float Speed, float Phase, Color Color);

    sealed record Fan(float X, float Height, float Width, float Phase, bool Front, bool RaisesHands, bool HasLight);

    sealed record Mote(float X, float Y, float Speed, float Size, float Phase);

    static readonly Color[] BeamColors =
    {
        new(170, 90, 255), new(60, 190, 255), new(255, 80, 170), new(255, 190, 70), new(90, 255, 200),
    };

    readonly Beam[] _beams;
    readonly Fan[] _crowd;
    readonly Mote[] _motes;
    readonly float _crowdBase;
    float _energy;

    /// <param name="crowdBase">Linha do chão da plateia; 0 = sem plateia.</param>
    public Stage(int seed, float crowdBase = 0)
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

        _crowdBase = crowdBase;
        _crowd = crowdBase <= 0 ? Array.Empty<Fan>() : Enumerable.Range(0, 2).SelectMany(row =>
            Enumerable.Range(0, 30).Select(i => new Fan(
                X: i * 44 + Next(-12, 12) + (row == 0 ? 22 : 0),
                Height: Next(row == 0 ? 52 : 36, row == 0 ? 72 : 56),
                Width: Next(26, 36),
                Phase: Next(0, 1),
                Front: row == 1,
                RaisesHands: random.NextDouble() < 0.45,
                HasLight: random.NextDouble() < 0.25))).ToArray();
    }

    /// <summary>Energia do show (0 a 1): vem do combo; sobe e desce suavemente.</summary>
    public float Energy => _energy;

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

    /// <param name="beatPhase">Posição dentro da batida (0 a 1): a plateia pula no começo dela.</param>
    public void DrawCrowd(Primitives p, double time, float beatPhase)
    {
        if (_crowd.Length == 0) return;
        foreach (var fan in _crowd.OrderBy(f => f.Front))
        {
            // Cada pessoa pula com um pequeno atraso próprio; quanto mais energia, mais alto.
            float jumpPhase = MathUtilF.Wrap(beatPhase - fan.Phase * 0.25f, 1);
            float jump = (2 + 12 * _energy) * MathF.Max(0, MathF.Sin(MathF.PI * jumpPhase));
            float sway = MathF.Sin((float)time * 1.3f + fan.Phase * 6) * 3;
            float x = fan.X + sway;
            float topY = _crowdBase - fan.Height - jump;
            var color = fan.Front ? new Color(8, 6, 16) : new Color(22, 16, 40);
            float half = fan.Width / 2;

            p.Quad(new(x - half, topY + 10), new(x + half, topY + 10), new(x + half + 4, _crowdBase), new(x - half - 4, _crowdBase), color);
            p.Disc(new Vector2(x, topY), half * 0.62f, color, 10);

            if (!fan.RaisesHands || _energy < 0.45f) continue;
            float raise = Math.Clamp((_energy - 0.45f) / 0.3f, 0, 1);
            float wave = MathF.Sin((float)time * 4 + fan.Phase * 5) * 6;
            var leftHand = new Vector2(x - half - 6 + wave, topY - 26 * raise);
            var rightHand = new Vector2(x + half + 6 + wave, topY - 30 * raise);
            p.Line(new Vector2(x - half + 3, topY + 14), leftHand, 6, color);
            p.Line(new Vector2(x + half - 3, topY + 14), rightHand, 6, color);

            // Isqueiros / celulares acesos quando o show esquenta.
            if (fan.HasLight && _energy > 0.6f)
            {
                float glow = 0.6f + 0.4f * MathF.Sin((float)time * 6 + fan.Phase * 9);
                p.Disc(rightHand - new Vector2(0, 6), 7, new Color(255, 200, 90) * (0.25f * glow), 8);
                p.Disc(rightHand - new Vector2(0, 6), 3, new Color(255, 235, 170) * glow, 6);
            }
        }
    }
}

static class MathUtilF
{
    /// <summary>Resto sempre positivo para floats (para posições que dão a volta na tela).</summary>
    public static float Wrap(float value, float range) => ((value % range) + range) % range;
}
