using FontStashSharp;
using GuitarPlay.Audio;
using GuitarPlay.Rendering;
using GuitarPlay.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay;

public enum Align { Left, Center, Right }

public sealed class GuitarPlayGame : Game
{
    public const int Width = 1280;
    public const int Height = 720;
    const int CircleTextureSize = 128;

    readonly GraphicsDeviceManager _graphics;
    // Teclas vindas dos eventos da janela: assim nenhum toque rápido se perde entre dois quadros.
    readonly HashSet<Keys> _held = new();
    readonly HashSet<Keys> _pressedQueue = new();
    readonly HashSet<Keys> _pressed = new();
    KeyboardState _previousKeys;
    IScreen _screen = null!;
    RenderTarget2D _canvas = null!;

    public GuitarPlayGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            SynchronizeWithVerticalRetrace = true,
            PreferMultiSampling = true,
        };
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        Window.Title = "Guitar Play";
        Window.AllowUserResizing = true;
        Window.KeyDown += (_, e) => { if (_held.Add(e.Key)) _pressedQueue.Add(e.Key); };
        Window.KeyUp += (_, e) => _held.Remove(e.Key);
    }

    protected override void Initialize()
    {
        // O jogo é desenhado em 1280x720 e escalado; a janela abre num tamanho que caiba no monitor.
        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        float fit = MathF.Min(1f, MathF.Min(display.Width * 0.92f / Width, display.Height * 0.85f / Height));
        _graphics.PreferredBackBufferWidth = (int)(Width * fit);
        _graphics.PreferredBackBufferHeight = (int)(Height * fit);
        _graphics.ApplyChanges();
        base.Initialize();
    }

    public SpriteBatch Batch { get; private set; } = null!;
    public Primitives Primitives { get; private set; } = null!;
    public Metronome Metronome { get; private set; } = null!;
    FontSystem Fonts { get; set; } = null!;
    FontSystem BoldFonts { get; set; } = null!;
    Texture2D CircleTexture { get; set; } = null!;
    Texture2D RingTexture { get; set; } = null!;

    protected override void LoadContent()
    {
        Batch = new SpriteBatch(GraphicsDevice);
        _canvas = new RenderTarget2D(GraphicsDevice, Width, Height, false, SurfaceFormat.Color, DepthFormat.None,
            preferredMultiSampleCount: 4, RenderTargetUsage.DiscardContents);
        Primitives = new Primitives(GraphicsDevice, Width, Height);
        Metronome = new Metronome();
        Fonts = LoadFonts("segoeui.ttf", "seguisym.ttf", "arial.ttf");
        BoldFonts = LoadFonts("segoeuib.ttf", "seguisym.ttf", "arialbd.ttf");
        CircleTexture = MakeDisc(innerRadius: 0);
        RingTexture = MakeDisc(innerRadius: CircleTextureSize / 2 - 10);
        ShowScreen(new MenuScreen(this));
    }

    public void ShowScreen(IScreen screen) => _screen = screen;

    /// <summary>Tecla apertada neste quadro (não conta se já estava segurada).</summary>
    public bool Pressed(Keys key) => _pressed.Contains(key);

    protected override void Update(GameTime gameTime)
    {
        // Une os eventos da janela com a leitura do teclado (cobre teclas que o evento não reporta).
        var keys = Keyboard.GetState();
        _pressed.Clear();
        _pressed.UnionWith(_pressedQueue);
        _pressedQueue.Clear();
        foreach (var key in keys.GetPressedKeys())
            if (!_previousKeys.IsKeyDown(key)) _pressed.Add(key);
        _previousKeys = keys;
        _screen.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_canvas);
        GraphicsDevice.Clear(Palette.BackgroundBottom);
        _screen.Draw(gameTime);
        GraphicsDevice.SetRenderTarget(null);

        // Escala mantendo 16:9, com tarjas pretas se a janela tiver outra proporção.
        GraphicsDevice.Clear(Color.Black);
        var viewport = GraphicsDevice.Viewport;
        float scale = MathF.Min(viewport.Width / (float)Width, viewport.Height / (float)Height);
        var size = new Vector2(Width, Height) * scale;
        var offset = (new Vector2(viewport.Width, viewport.Height) - size) / 2;
        Batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
        Batch.Draw(_canvas, new Rectangle((int)offset.X, (int)offset.Y, (int)size.X, (int)size.Y), Color.White);
        Batch.End();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        Metronome.Dispose();
        base.UnloadContent();
    }

    public void DrawBackground() =>
        Primitives.VerticalGradient(0, 0, Width, Height, Palette.BackgroundTop, Palette.BackgroundBottom);

    public void Circle(Vector2 center, float radius, Color color) => DrawDisc(CircleTexture, center, radius, color);

    public void Ring(Vector2 center, float radius, Color color) => DrawDisc(RingTexture, center, radius, color);

    /// <summary>Escreve texto e devolve o tamanho ocupado. Precisa estar entre Batch.Begin/End.</summary>
    public Vector2 Text(string text, Vector2 position, float size, Color color,
        bool bold = false, Align align = Align.Left, bool middle = false, bool shadow = false)
    {
        var font = (bold ? BoldFonts : Fonts).GetFont(Math.Max(6, (int)MathF.Round(size)));
        var measured = font.MeasureString(text);
        if (align == Align.Center) position.X -= measured.X / 2;
        else if (align == Align.Right) position.X -= measured.X;
        if (middle) position.Y -= measured.Y / 2;
        position = new Vector2(MathF.Round(position.X), MathF.Round(position.Y));

        if (shadow) Batch.DrawString(font, text, position + new Vector2(1, 2), Color.Black * (color.A / 255f * 0.7f));
        Batch.DrawString(font, text, position, color);
        return measured;
    }

    void DrawDisc(Texture2D texture, Vector2 center, float radius, Color color)
    {
        var origin = new Vector2(CircleTextureSize / 2f);
        Batch.Draw(texture, center, null, color, 0f, origin, radius / (CircleTextureSize / 2f), SpriteEffects.None, 0f);
    }

    Texture2D MakeDisc(float innerRadius)
    {
        const int size = CircleTextureSize;
        float outer = size / 2f - 1;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f));
            float alpha = Math.Clamp(outer - distance, 0, 1);
            if (innerRadius > 0) alpha *= Math.Clamp(distance - innerRadius, 0, 1);
            pixels[y * size + x] = Color.White * alpha;
        }
        var texture = new Texture2D(GraphicsDevice, size, size);
        texture.SetData(pixels);
        return texture;
    }

    static FontSystem LoadFonts(params string[] files)
    {
        var system = new FontSystem();
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        foreach (string file in files)
        {
            string path = Path.Combine(folder, file);
            if (File.Exists(path)) system.AddFont(File.ReadAllBytes(path));
        }
        return system;
    }
}
