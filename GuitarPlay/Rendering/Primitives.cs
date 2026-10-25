using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GuitarPlay.Rendering;

/// <summary>Desenha polígonos coloridos em coordenadas de tela (a pista em perspectiva é feita com isso).</summary>
public sealed class Primitives
{
    readonly GraphicsDevice _device;
    readonly BasicEffect _effect;
    readonly List<VertexPositionColor> _vertices = new();

    public Primitives(GraphicsDevice device, int width, int height)
    {
        _device = device;
        _effect = new BasicEffect(device)
        {
            VertexColorEnabled = true,
            Projection = Matrix.CreateOrthographicOffCenter(0, width, height, 0, 0, 1),
        };
    }

    public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd)
    {
        Add(a, ca); Add(b, cb); Add(c, cc);
        Add(a, ca); Add(c, cc); Add(d, cd);
    }

    public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color) => Quad(a, b, c, d, color, color, color, color);

    public void Rect(float x, float y, float width, float height, Color color) =>
        Quad(new(x, y), new(x + width, y), new(x + width, y + height), new(x, y + height), color);

    public void Line(Vector2 from, Vector2 to, float thickness, Color color)
    {
        var direction = to - from;
        if (direction.LengthSquared() < 0.01f) return;
        direction.Normalize();
        var normal = new Vector2(-direction.Y, direction.X) * (thickness / 2);
        Quad(from + normal, to + normal, to - normal, from - normal, color);
    }

    /// <summary>Círculo cheio feito de triângulos (para cabeças da plateia e poeira de luz).</summary>
    public void Disc(Vector2 center, float radius, Color color, int segments = 12)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathHelper.TwoPi * i / segments, a1 = MathHelper.TwoPi * (i + 1) / segments;
            Add(center, color);
            Add(center + radius * new Vector2(MathF.Cos(a0), MathF.Sin(a0)), color);
            Add(center + radius * new Vector2(MathF.Cos(a1), MathF.Sin(a1)), color);
        }
    }

    public void VerticalGradient(float x, float y, float width, float height, Color top, Color bottom) =>
        Quad(new(x, y), new(x + width, y), new(x + width, y + height), new(x, y + height), top, top, bottom, bottom);

    public void Flush()
    {
        if (_vertices.Count == 0) return;
        _device.BlendState = BlendState.AlphaBlend;
        _device.RasterizerState = RasterizerState.CullNone;
        _device.DepthStencilState = DepthStencilState.None;
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleList, _vertices.ToArray(), 0, _vertices.Count / 3);
        }
        _vertices.Clear();
    }

    void Add(Vector2 position, Color color) => _vertices.Add(new VertexPositionColor(new Vector3(position, 0), color));
}
