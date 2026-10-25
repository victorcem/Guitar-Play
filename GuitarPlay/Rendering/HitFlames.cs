using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GuitarPlay.Rendering;

/// <summary>Chama animada que sobe do alvo a cada acerto (quadros "muzzle" do Kenney Particle Pack).</summary>
public sealed class HitFlames
{
    const float Life = 0.42f;

    sealed class Flame
    {
        public required Vector2 Position { get; init; }
        public required Color Color { get; init; }
        public required float Size { get; init; }
        public float Age { get; set; }
    }

    readonly Texture2D[] _frames;
    readonly Texture2D _flash;
    readonly List<Flame> _flames = new();

    public HitFlames(Assets assets)
    {
        _frames = Enumerable.Range(1, 5).Select(i => assets.Texture($"Fx/muzzle_0{i}.png")).ToArray();
        _flash = assets.Texture("Fx/star_07.png");
    }

    /// <param name="size">Altura da chama em pixels.</param>
    public void Add(Vector2 position, Color color, float size) =>
        _flames.Add(new Flame { Position = position, Color = color, Size = size });

    public void Update(float dt)
    {
        foreach (var flame in _flames) flame.Age += dt;
        _flames.RemoveAll(f => f.Age > Life);
    }

    /// <summary>Desenhar entre Batch.Begin/End com BlendState.Additive.</summary>
    public void Draw(SpriteBatch batch)
    {
        foreach (var flame in _flames)
        {
            float k = flame.Age / Life;
            var frame = _frames[Math.Min(_frames.Length - 1, (int)(k * _frames.Length))];
            // Sobe rápido, afina e apaga.
            float height = flame.Size * (0.6f + 0.6f * MathF.Sqrt(k));
            float fade = 1 - k * k;
            var origin = new Vector2(frame.Width / 2f, frame.Height * 0.85f);
            batch.Draw(frame, flame.Position, null, flame.Color * fade, 0, origin,
                new Vector2(0.7f - 0.3f * k, 1) * (height / frame.Height), SpriteEffects.None, 0);
            batch.Draw(frame, flame.Position, null, Color.White * (0.7f * fade), 0, origin,
                new Vector2(0.35f, 0.6f) * (height / frame.Height), SpriteEffects.None, 0);
            if (k < 0.3f)
            {
                float flash = 1 - k / 0.3f;
                batch.Draw(_flash, flame.Position, null, Color.Lerp(flame.Color, Color.White, 0.5f) * flash, k * 2,
                    new Vector2(_flash.Width, _flash.Height) / 2, flame.Size * 1.2f / _flash.Width, SpriteEffects.None, 0);
            }
        }
    }
}
