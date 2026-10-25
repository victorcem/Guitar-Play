using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GuitarPlay.Rendering;

/// <summary>
/// Pós-processamento de brilho (shader Content/Effects/Bloom.fx). A cena é desenhada em
/// <see cref="Begin"/>…<see cref="End"/>; o que vem depois (HUD, textos) fica sem brilho.
/// </summary>
public sealed class Bloom
{
    readonly GraphicsDevice _device;
    readonly SpriteBatch _batch;
    readonly Effect _effect;
    readonly RenderTarget2D _scene;
    readonly RenderTarget2D _halfA, _halfB, _quarterA, _quarterB;

    public Bloom(GraphicsDevice device, SpriteBatch batch, Assets assets, int width, int height)
    {
        _device = device;
        _batch = batch;
        _effect = assets.Effect("Effects/Bloom.mgfxo");
        // A cena tem profundidade (o palco é 3D) e antisserrilhado, como a tela do jogo.
        _scene = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24,
            preferredMultiSampleCount: 4, RenderTargetUsage.DiscardContents);
        _halfA = Target(width / 2, height / 2);
        _halfB = Target(width / 2, height / 2);
        _quarterA = Target(width / 4, height / 4);
        _quarterB = Target(width / 4, height / 4);

        RenderTarget2D Target(int w, int h) => new(device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
    }

    /// <summary>0 desliga o brilho; 1 é o normal. Sobe no x4.</summary>
    public float Strength { get; set; } = 1;

    public float Threshold { get; set; } = 0.62f;

    public void Begin()
    {
        _device.SetRenderTarget(_scene);
        _device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Palette.BackgroundBottom, 1, 0);
    }

    /// <summary>Aplica o brilho e desenha o resultado em <paramref name="target"/>.</summary>
    public void End(RenderTarget2D target)
    {
        _effect.Parameters["Threshold"].SetValue(Threshold);
        Pass(_scene, _halfA, "Bright");
        Blur(_halfA, _halfB);
        Pass(_halfA, _quarterA, null);
        Blur(_quarterA, _quarterB);

        _device.SetRenderTarget(target);
        _effect.CurrentTechnique = _effect.Techniques["Combine"];
        _effect.Parameters["BloomNear"].SetValue(_halfA);
        _effect.Parameters["BloomFar"].SetValue(_quarterA);
        _effect.Parameters["NearIntensity"].SetValue(0.85f * Strength);
        _effect.Parameters["FarIntensity"].SetValue(0.75f * Strength);
        _batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, effect: _effect);
        _batch.Draw(_scene, new Rectangle(0, 0, target.Width, target.Height), Color.White);
        _batch.End();
    }

    void Blur(RenderTarget2D a, RenderTarget2D b)
    {
        // Duas idas e voltas (horizontal, vertical) deixam o halo largo e macio.
        for (int i = 0; i < 2; i++)
        {
            _effect.Parameters["TexelStep"].SetValue(new Vector2(1f / a.Width, 0));
            Pass(a, b, "Blur");
            _effect.Parameters["TexelStep"].SetValue(new Vector2(0, 1f / a.Height));
            Pass(b, a, "Blur");
        }
    }

    /// <param name="technique">null = cópia simples (reduz a resolução).</param>
    void Pass(Texture2D source, RenderTarget2D destination, string? technique)
    {
        _device.SetRenderTarget(destination);
        Effect? effect = null;
        if (technique is not null)
        {
            _effect.CurrentTechnique = _effect.Techniques[technique];
            effect = _effect;
        }
        _batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, effect: effect);
        _batch.Draw(source, new Rectangle(0, 0, destination.Width, destination.Height), Color.White);
        _batch.End();
    }
}
