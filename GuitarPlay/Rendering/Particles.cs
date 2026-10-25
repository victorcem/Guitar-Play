using Microsoft.Xna.Framework;

namespace GuitarPlay.Rendering;

/// <summary>
/// Sistema simples de partículas (faíscas, chamas, confete). Desenhar entre
/// Batch.Begin/End com BlendState.Additive para o efeito de brilho.
/// </summary>
public sealed class Particles
{
    struct Particle
    {
        public Vector2 Position, Velocity;
        public float Life, MaxLife, Size, Gravity;
        public Color Color;
    }

    readonly List<Particle> _items = new();
    readonly Random _random = new();

    public int Count => _items.Count;

    public float Next(float min, float max) => min + (float)_random.NextDouble() * (max - min);

    public void Emit(Vector2 position, Vector2 velocity, Color color, float life, float size, float gravity = 0) =>
        _items.Add(new Particle
        {
            Position = position, Velocity = velocity, Color = color,
            Life = life, MaxLife = life, Size = size, Gravity = gravity,
        });

    /// <summary>Explosão de faíscas para cima, num cone.</summary>
    public void Burst(Vector2 at, Color color, int count, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = -MathHelper.PiOver2 + Next(-1.1f, 1.1f);
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed * Next(0.4f, 1f);
            Emit(at, velocity, color, Next(0.35f, 0.7f), Next(2.5f, 5f), gravity: 700);
        }
    }

    public void Update(float dt)
    {
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            var p = _items[i];
            p.Life -= dt;
            if (p.Life <= 0)
            {
                _items.RemoveAt(i);
                continue;
            }
            p.Velocity.Y += p.Gravity * dt;
            p.Velocity *= MathF.Pow(0.2f, dt);
            p.Position += p.Velocity * dt;
            _items[i] = p;
        }
    }

    public void Draw(GuitarPlayGame game)
    {
        foreach (var p in _items)
        {
            float fade = p.Life / p.MaxLife;
            game.Circle(p.Position, p.Size * (0.4f + 0.6f * fade), p.Color * fade);
        }
    }
}
