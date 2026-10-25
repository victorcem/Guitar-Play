using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GuitarPlay.Rendering;

/// <summary>
/// Palco 2.5D da Pista. Ao fundo, o palco em 3D (treliça, parede de LED, caixas, refletores) com
/// luzes que trocam de cor no compasso; na frente, camadas 2D com parallax: fachos de luz, fumaça,
/// pirotecnia e três fileiras de plateia que pulam na batida e se animam com o combo.
/// Arte CC0 (iPoly3D e Kenney), ver CREDITS.md.
/// </summary>
public sealed class Stage
{
    sealed record Prop(ObjMesh Mesh, Matrix World);

    /// <param name="Up">Refletor de chão no fundo do palco, com o facho subindo para o céu.</param>
    sealed record Fixture(Vector3 Position, float Speed, float Phase, int ColorSlot, bool Up = false);

    sealed record Puff(Vector3 Position, float Size, float Speed, float Spin, int Texture, float Phase);

    sealed record Fan(float X, int Row, int Character, float Phase, bool HasLight, float Size);

    // Cores das lavagens de luz; o compasso escolhe o par da vez.
    static readonly Color[] ShowColors =
    {
        new(255, 60, 170), new(60, 190, 255), new(255, 170, 50), new(160, 80, 255), new(255, 70, 60), new(70, 255, 190),
    };

    /// <summary>Cor "elétrica" do x4, como o Star Power do Guitar Hero.</summary>
    public static readonly Color Electric = new(90, 190, 255);

    static readonly string[] Characters = { "malePerson", "femalePerson", "maleAdventurer", "femaleAdventurer" };
    static readonly string[] CalmPoses = { "idle", "show", "wide" };

    // Fileiras da plateia (do fundo para a frente): escala do sprite, linha do chão, espaçamento, parallax.
    static readonly (float Scale, float Base, float Spacing, float Parallax, Color Body)[] Rows =
    {
        (0.40f, 566, 44, 5, new Color(26, 20, 44)),
        (0.52f, 604, 58, 9, new Color(15, 12, 28)),
        (0.68f, 652, 78, 15, new Color(5, 4, 10)),
    };

    const float AspectRatio = GuitarPlayGame.Width / (float)GuitarPlayGame.Height;
    // O palco fica centrado atrás da pista (x = 450), não no meio da tela: a projeção é deslocada.
    const float ScreenCenterX = 450;

    readonly GraphicsDevice _device;
    readonly BasicEffect _lit;
    readonly BasicEffect _glow;
    readonly BasicEffect _screen;
    readonly Texture2D _albedo, _glowMap, _beam, _pixel;
    readonly Texture2D[] _flames, _smoke, _bolts;
    readonly Texture2D _flare, _halo, _star;
    readonly Dictionary<string, (Texture2D Color, Texture2D Mask)> _poses = new();
    readonly RenderTarget2D _ledWall;
    readonly VertexPositionTexture[] _ledQuad;
    readonly List<Prop> _props = new();
    readonly Fixture[] _fixtures;
    readonly Vector3[] _pyroSpots;
    readonly Puff[] _puffs;
    readonly Fan[] _crowd;
    readonly Particles _sparks = new();
    readonly Random _random;

    Matrix _view, _projection;
    Vector3 _cameraOffset;
    float _energy, _starPower, _pyro, _shake;
    int _lastBeat = int.MinValue;
    double _time;
    float _beatPhase, _pulse;

    public Stage(GraphicsDevice device, Assets assets, int seed = 3)
    {
        _device = device;
        _random = new Random(seed);
        _albedo = assets.Texture("Stage/albedo.png");
        _glowMap = assets.Texture("Stage/glow.png");
        _flames = Enumerable.Range(1, 5).Select(i => assets.Texture($"Fx/muzzle_0{i}.png")).ToArray();
        _smoke = new[] { "smoke_04", "smoke_07", "smoke_08" }.Select(n => assets.Texture($"Fx/{n}.png")).ToArray();
        _bolts = Enumerable.Range(1, 4).Select(i => assets.Texture($"Fx/spark_0{i}.png")).ToArray();
        _flare = assets.Texture("Fx/flare_01.png");
        _halo = assets.Texture("Fx/light_01.png");
        _star = assets.Texture("Fx/star_07.png");
        _beam = MakeBeamTexture(device);
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        foreach (string character in Characters)
        foreach (string pose in CalmPoses.Concat(new[] { "cheer0", "cheer1", "jump" }))
        {
            string file = $"Crowd/{character}_{pose}.png";
            _poses[$"{character}_{pose}"] = (assets.Texture(file), assets.Mask(file));
        }

        _lit = new BasicEffect(device)
        {
            TextureEnabled = true, Texture = _albedo, LightingEnabled = true, PreferPerPixelLighting = true,
            FogEnabled = true, FogStart = 32, FogEnd = 80, FogColor = new Vector3(0.03f, 0.02f, 0.06f),
            SpecularPower = 24,
        };
        _glow = new BasicEffect(device) { TextureEnabled = true, Texture = _glowMap };
        _screen = new BasicEffect(device) { TextureEnabled = true };

        // Montagem igual à da cena original do pacote (concert.gltf), com o centro do palco na origem.
        Add("stage.005", Vector3.Zero);
        Add("background.001", new Vector3(0, 0, -9.13f), new Vector3(1, 1, 0.03f));
        Add("truss_structure.001", new Vector3(0, 0, 0.1f));
        foreach (float side in new[] { -1f, 1f })
        {
            Add("speaker.005", new Vector3(side * 5.7f, 2, 6.97f));
            Add("speaker.006", new Vector3(side * 9.8f, 2, 6.98f));
            Add("speaker.006", new Vector3(side * 11.3f, 2, 6.97f));
            Add("speaker.004", new Vector3(side * 7.2f, 2, -0.9f));
            Add("speaker.001", new Vector3(side * 12.6f, 0, 10.6f));
            Add("spotlight.005", new Vector3(side * 3.2f, 2, 7.9f));
        }
        Add("mic.001", new Vector3(0.3f, 2, 6.2f));
        for (int i = 0; i < 9; i++) Add("barricade", new Vector3(-12 + i * 3, 0, 13.5f));

        string[] hanging = { "spotlight.004", "spotlight.001", "spotlight.003", "spotlight.002" };
        _fixtures = new[] { -11.5f, -7.5f, -3.5f, 3.5f, 7.5f, 11.5f }.Select((x, i) =>
        {
            var position = new Vector3(x, 13.1f, 9.3f);
            Add(hanging[i % hanging.Length], position);
            return new Fixture(position - new Vector3(0, 0.8f, -0.3f), 0.5f + 0.13f * i, i * 1.7f, i % 2);
        }).Concat(new[] { -10f, -6f, -2f, 2f, 6f, 10f }.Select((x, i) =>
        {
            var position = new Vector3(x, 2, -7.6f);
            Add("spotlight.005", position);
            return new Fixture(position + new Vector3(0, 0.5f, 0), 0.35f + 0.09f * i, i * 2.3f, (i + 1) % 2, Up: true);
        })).ToArray();

        _pyroSpots = new[] { -11f, -5.2f, 5.2f, 11f }.Select(x => new Vector3(x, 2, 8.3f)).ToArray();

        _puffs = Enumerable.Range(0, 14).Select(i => new Puff(
            new Vector3(Next(-15, 15), Next(1.6f, 4.5f), Next(2, 12)), Next(6, 11), Next(0.25f, 0.7f) * (i % 2 == 0 ? 1 : -1),
            Next(-0.15f, 0.15f), i % _smoke.Length, Next(0, MathHelper.TwoPi))).ToArray();

        _crowd = Rows.SelectMany((row, r) =>
        {
            int count = (int)(GuitarPlayGame.Width / row.Spacing) + 3;
            return Enumerable.Range(0, count).Select(i => new Fan(
                X: -row.Spacing + i * row.Spacing + Next(-0.3f, 0.3f) * row.Spacing + (r % 2) * row.Spacing / 2,
                Row: r, Character: _random.Next(Characters.Length), Phase: Next(0, 1),
                HasLight: _random.NextDouble() < 0.18, Size: Next(0.9f, 1.08f)));
        }).ToArray();

        _ledWall = new RenderTarget2D(device, 176, 80, false, SurfaceFormat.Color, DepthFormat.None);
        const float ledZ = -8.95f;
        _ledQuad = new[]
        {
            new VertexPositionTexture(new Vector3(-11.6f, 12.4f, ledZ), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(11.6f, 12.4f, ledZ), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(-11.6f, 2.4f, ledZ), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(11.6f, 2.4f, ledZ), new Vector2(1, 1)),
        };

        void Add(string name, Vector3 position, Vector3? scale = null) =>
            _props.Add(new Prop(assets.Mesh($"Stage/{name}.obj"), Matrix.CreateScale(scale ?? Vector3.One) * Matrix.CreateTranslation(position)));
    }

    float Next(float min, float max) => min + (float)_random.NextDouble() * (max - min);

    /// <summary>Energia do show (0 a 1): vem do combo; sobe e desce suavemente.</summary>
    public float Energy => _energy;

    /// <summary>Multiplicador no máximo: luz elétrica, pirotecnia e câmera balançando.</summary>
    public bool StarPower { get; set; }

    /// <summary>Quanto o brilho deve subir agora (0 a 1), para o bloom acompanhar o show.</summary>
    public float Intensity => 0.35f * _energy + 0.4f * _starPower + 0.25f * _pulse;

    /// <summary>Volta ao começo do show (plateia calma, sem x4) para um novo nível.</summary>
    public void Reset()
    {
        _energy = _starPower = _pyro = _shake = 0;
        StarPower = false;
        _lastBeat = int.MinValue;
        _sparks.Clear();
    }

    /// <summary>Solta a pirotecnia do palco por um instante (marcos de combo).</summary>
    public void Kick() => _pyro = 1;

    /// <param name="beat">Número da batida atual (para trocar as luzes e pular na hora certa).</param>
    /// <param name="beatPhase">Posição dentro da batida (0 a 1).</param>
    public void Update(float dt, double time, int beat, float beatPhase, float targetEnergy)
    {
        _time = time;
        _beatPhase = beatPhase;
        _pulse = MathF.Exp(-beatPhase * 5);
        _energy += (Math.Clamp(targetEnergy, 0, 1) - _energy) * MathF.Min(1, dt * 1.5f);
        _starPower += ((StarPower ? 1 : 0) - _starPower) * MathF.Min(1, dt * 3);
        _pyro = MathF.Max(0, _pyro - dt * 1.1f);
        _shake += ((StarPower ? 1 : 0) - _shake) * MathF.Min(1, dt * 2);

        if (beat != _lastBeat)
        {
            _lastBeat = beat;
            // No x4, cada batida solta um chafariz de faíscas das bocas de fogo.
            if (StarPower || _pyro > 0.3f)
                foreach (var spot in _pyroSpots) Fountain(spot, StarPower ? Electric : new Color(255, 170, 60));
        }
        _sparks.Update(dt);

        // Balanço lento sempre (dá vida ao parallax); no x4, tremor leve no ritmo.
        float t = (float)time;
        _cameraOffset = new Vector3(MathF.Sin(t * 0.21f) * 0.9f, MathF.Sin(t * 0.33f) * 0.35f, 0);
        if (_shake > 0.01f)
            _cameraOffset += _shake * new Vector3(
                0.22f * MathF.Sin(t * 13.1f) + 0.12f * MathF.Sin(t * 29.7f),
                0.18f * MathF.Sin(t * 17.3f) + 0.25f * _pulse,
                0);

        var eye = new Vector3(0, 3.2f, 40) + _cameraOffset;
        _view = Matrix.CreateLookAt(eye, new Vector3(_cameraOffset.X * 0.4f, 8.8f, 0), Vector3.Up);
        float shift = (ScreenCenterX - GuitarPlayGame.Width / 2f) / (GuitarPlayGame.Width / 2f);
        _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(46), AspectRatio, 0.5f, 200)
            * Matrix.CreateTranslation(shift, 0, 0);
    }

    void Fountain(Vector3 spot, Color color)
    {
        var (at, ppu) = Project(spot);
        for (int i = 0; i < 14; i++)
        {
            float angle = -MathHelper.PiOver2 + Next(-0.35f, 0.35f);
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * ppu * Next(14, 26);
            _sparks.Emit(at, velocity, Color.Lerp(color, Color.White, Next(0, 0.5f)), Next(0.5f, 0.9f), Next(1.5f, 3f), gravity: ppu * 30);
        }
    }

    Color Wash(int slot)
    {
        // Um par de cores por compasso; no x4 tudo puxa para o azul elétrico.
        int bar = (int)MathF.Floor(_lastBeat / 4f);
        var color = ShowColors[MathUtil.Mod(bar * 2 + slot, ShowColors.Length)];
        return Color.Lerp(color, slot == 0 ? Electric : Color.Lerp(Electric, Color.White, 0.5f), _starPower);
    }

    /// <summary>Desenha a parede de LED; chamar antes de começar a cena (troca de render target).</summary>
    public void PrepareFrame(SpriteBatch batch)
    {
        _device.SetRenderTarget(_ledWall);
        _device.Clear(new Color(4, 3, 10));
        float t = (float)_time;
        float level = 0.6f + 0.4f * _energy + 0.3f * _pulse;
        var center = new Vector2(_ledWall.Width / 2f, _ledWall.Height / 2f);
        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        // Equalizador: barras que sobem na batida, em degradê das duas cores do compasso.
        const int bars = 22;
        float barWidth = _ledWall.Width / (float)bars;
        for (int i = 0; i < bars; i++)
        {
            float wobble = 0.5f + 0.25f * MathF.Sin(t * 3.1f + i * 0.9f) + 0.25f * MathF.Sin(t * 5.3f + i * 2.3f);
            float height = _ledWall.Height * (0.15f + 0.55f * wobble * (0.45f + 0.55f * _pulse) * (0.6f + 0.4f * _energy));
            var color = Color.Lerp(Wash(0), Wash(1), i / (bars - 1f)) * (0.55f * level);
            batch.Draw(_pixel, new Rectangle((int)(i * barWidth) + 1, (int)(_ledWall.Height - height), (int)barWidth - 2, (int)height), color);
        }
        // Dois "sóis" girando nas cores do compasso, pulsando na batida.
        for (int i = 0; i < 2; i++)
        {
            float angle = t * (i == 0 ? 0.6f : -0.45f);
            float size = (0.24f + 0.06f * _pulse) * (i == 0 ? 1 : 1.5f);
            DrawCentered(batch, i == 0 ? _star : _halo, center, Wash(i) * level, angle, size);
        }
        // Faixas de luz que atravessam a parede.
        for (int i = 0; i < 3; i++)
        {
            float x = MathUtilF.Wrap(t * 40 + i * 70, _ledWall.Width + 60) - 30;
            batch.Draw(_beam, new Rectangle((int)x, 0, 10, _ledWall.Height), Wash(i % 2) * (0.5f * level));
        }
        if (_starPower > 0.05f)
        {
            // Raios no x4.
            var bolt = _bolts[(int)(t * 12) % _bolts.Length];
            DrawCentered(batch, bolt, center + new Vector2(MathF.Sin(t * 7) * 30, 0), Electric * _starPower, t * 3, 0.32f);
        }
        batch.End();
    }

    static void DrawCentered(SpriteBatch batch, Texture2D texture, Vector2 at, Color color, float rotation, float scale) =>
        batch.Draw(texture, at, null, color, rotation, new Vector2(texture.Width, texture.Height) / 2, scale, SpriteEffects.None, 0);

    /// <summary>Palco 3D + luzes e fumaça. Precisa de um render target com profundidade.</summary>
    public void DrawStage(GuitarPlayGame game)
    {
        var p = game.Primitives;
        var sky = Color.Lerp(new Color(20, 12, 40), new Color(8, 20, 48), _starPower);
        p.VerticalGradient(0, 0, GuitarPlayGame.Width, GuitarPlayGame.Height, sky, Palette.BackgroundBottom);
        p.Flush();

        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;
        _device.RasterizerState = RasterizerState.CullNone;
        _device.SamplerStates[0] = SamplerState.PointClamp;

        float level = 0.7f + 0.35f * _energy + 0.3f * _pulse;
        _lit.View = _glow.View = _screen.View = _view;
        _lit.Projection = _glow.Projection = _screen.Projection = _projection;
        _lit.AmbientLightColor = new Vector3(0.1f, 0.08f, 0.14f);
        SetLight(_lit.DirectionalLight0, new Vector3(-0.4f, -0.6f, -1), Wash(0), level);
        SetLight(_lit.DirectionalLight1, new Vector3(0.5f, -0.5f, -0.8f), Wash(1), level);
        SetLight(_lit.DirectionalLight2, new Vector3(0, -0.3f, 1), Color.White, 0.25f);
        foreach (var prop in _props)
        {
            _lit.World = prop.World;
            prop.Mesh.Draw(_device, _lit);
        }

        // Fitas de LED do palco (mapa de brilho do pacote), somadas por cima.
        _device.BlendState = BlendState.Additive;
        _device.DepthStencilState = DepthStencilState.DepthRead;
        _glow.DiffuseColor = Color.Lerp(Wash(1), Color.White, 0.4f).ToVector3() * (0.6f + 0.6f * _pulse);
        foreach (var prop in _props)
        {
            _glow.World = prop.World;
            prop.Mesh.Draw(_device, _glow);
        }

        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;
        _screen.Texture = _ledWall;
        _screen.World = Matrix.Identity;
        foreach (var pass in _screen.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _ledQuad, 0, 2);
        }
        _device.DepthStencilState = DepthStencilState.None;

        var batch = game.Batch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        DrawBeams(batch, level);
        DrawSmoke(batch);
        DrawPyro(batch);
        batch.End();
    }

    static void SetLight(DirectionalLight light, Vector3 direction, Color color, float strength)
    {
        light.Enabled = true;
        light.Direction = Vector3.Normalize(direction);
        light.DiffuseColor = color.ToVector3() * strength;
        light.SpecularColor = color.ToVector3() * (strength * 0.6f);
    }

    /// <summary>Ponto 3D na tela, com quantos pixels vale uma unidade do mundo naquela distância.</summary>
    (Vector2 Screen, float PixelsPerUnit) Project(Vector3 world)
    {
        var clip = Vector4.Transform(new Vector4(world, 1), _view * _projection);
        float w = MathF.Max(0.01f, clip.W);
        var screen = new Vector2((clip.X / w + 1) / 2 * GuitarPlayGame.Width, (1 - clip.Y / w) / 2 * GuitarPlayGame.Height);
        return (screen, _projection.M22 * GuitarPlayGame.Height / 2 / w);
    }

    void DrawBeams(SpriteBatch batch, float level)
    {
        float t = (float)_time;
        foreach (var fixture in _fixtures)
        {
            // Cada refletor varre o palco e a plateia (ou o céu, os de chão); o facho abre em cone.
            float sweep = MathF.Sin(t * fixture.Speed + fixture.Phase);
            var target = fixture.Up
                ? new Vector3(fixture.Position.X * 2.2f + sweep * 10, 34, -16)
                : new Vector3(fixture.Position.X * 1.3f + sweep * 7, 0, 16 + MathF.Cos(t * fixture.Speed * 0.7f + fixture.Phase) * 5);
            var (from, ppuFrom) = Project(fixture.Position);
            var (to, ppuTo) = Project(target);
            var color = Wash(fixture.ColorSlot);
            float alpha = (0.3f + 0.15f * _energy + 0.15f * _pulse + 0.15f * _starPower) * level;

            var axis = to - from;
            float length = axis.Length();
            float rotation = MathF.Atan2(axis.Y, axis.X) - MathHelper.PiOver2;
            float width = (fixture.Up ? 7 : 5.5f) * ppuTo;
            batch.Draw(_beam, from, null, color * alpha, rotation, new Vector2(_beam.Width / 2f, 0),
                new Vector2(width / _beam.Width, length / _beam.Height), SpriteEffects.None, 0);

            // Brilho do aparelho (some com o bloom e vira "estrela").
            float glare = 0.55f + 0.45f * _pulse;
            DrawCentered(batch, _halo, from, color * (0.5f * glare), 0, 2.8f * ppuFrom / _halo.Width);
            DrawCentered(batch, _flare, from, Color.White * (0.6f * glare), t * 0.3f + fixture.Phase, 3.2f * ppuFrom / _flare.Width);
        }
    }

    void DrawSmoke(SpriteBatch batch)
    {
        float t = (float)_time;
        foreach (var puff in _puffs)
        {
            var position = puff.Position;
            position.X = MathUtilF.Wrap(position.X + 15 + t * puff.Speed, 30) - 15;
            position.Y += MathF.Sin(t * 0.4f + puff.Phase) * 0.3f;
            var (at, ppu) = Project(position);
            float breathe = 0.85f + 0.15f * MathF.Sin(t * 0.6f + puff.Phase);
            var tint = Color.Lerp(Wash(puff.Texture % 2), Color.White, 0.25f) * ((0.1f + 0.06f * _energy + 0.05f * _starPower) * breathe);
            var texture = _smoke[puff.Texture];
            DrawCentered(batch, texture, at, tint, puff.Phase + t * puff.Spin, puff.Size * ppu / texture.Width);
        }
    }

    void DrawPyro(SpriteBatch batch)
    {
        float t = (float)_time;
        float fire = MathF.Max(_starPower, _pyro);
        if (fire > 0.02f)
        {
            var hot = Color.Lerp(new Color(255, 140, 40), Electric, _starPower);
            for (int i = 0; i < _pyroSpots.Length; i++)
            {
                // Labareda: alterna os quadros do "muzzle" e cresce na batida.
                var (at, ppu) = Project(_pyroSpots[i]);
                var texture = _flames[(int)(t * 18 + i * 2) % _flames.Length];
                float height = (8 + 4 * _pulse) * ppu * fire;
                var origin = new Vector2(texture.Width / 2f, texture.Height * 0.85f);
                batch.Draw(texture, at, null, hot * 0.9f, 0, origin, new Vector2(0.55f, 1) * (height / texture.Height), SpriteEffects.None, 0);
                batch.Draw(texture, at, null, Color.White * 0.5f, 0, origin, new Vector2(0.3f, 0.7f) * (height / texture.Height), SpriteEffects.None, 0);
            }
        }
    }

    /// <summary>Plateia em silhueta contra a luz do palco, em três fileiras com parallax.</summary>
    public void DrawCrowd(GuitarPlayGame game)
    {
        var batch = game.Batch;
        float t = (float)_time;
        var rim = Color.Lerp(Wash(0), Color.White, 0.25f);

        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        _sparks.Draw(game);
        batch.End();

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        var lights = new List<Vector2>();
        foreach (var fan in _crowd)
        {
            var row = Rows[fan.Row];
            // Cada pessoa pula com um pequeno atraso próprio; quanto mais energia, mais alto.
            float jumpPhase = MathUtilF.Wrap(_beatPhase - fan.Phase * 0.2f, 1);
            float jump = (1 + 16 * _energy * _energy) * MathF.Max(0, MathF.Sin(MathF.PI * jumpPhase)) * row.Scale;
            string pose = PoseOf(fan);
            var (color, mask) = _poses[$"{Characters[fan.Character]}_{pose}"];
            float scale = row.Scale * fan.Size;
            float x = fan.X - _cameraOffset.X * row.Parallax + MathF.Sin(t * 1.1f + fan.Phase * 6) * 3;
            var feet = new Vector2(x, row.Base - jump);
            var origin = new Vector2(color.Width / 2f, color.Height);

            // Contorno iluminado (a luz vem do palco, de cima) e o corpo escuro por cima.
            float rimAlpha = 0.35f + 0.35f * _energy + 0.3f * _pulse;
            batch.Draw(mask, feet + new Vector2(-1.5f, -3), null, rim * rimAlpha, 0, origin, scale, SpriteEffects.None, 0);
            batch.Draw(mask, feet + new Vector2(1.5f, -2), null, Wash(1) * (rimAlpha * 0.7f), 0, origin, scale, SpriteEffects.None, 0);
            batch.Draw(mask, feet, null, row.Body, 0, origin, scale, SpriteEffects.None, 0);
            if (fan.Row == 0) batch.Draw(color, feet, null, Color.White * 0.12f, 0, origin, scale, SpriteEffects.None, 0);

            if (fan.HasLight && _energy > 0.55f && pose is "cheer1" or "jump")
                lights.Add(feet - new Vector2(-color.Width * 0.3f, color.Height * 0.95f) * scale);
        }
        batch.End();

        // Isqueiros / celulares acesos quando o show esquenta.
        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        foreach (var at in lights)
        {
            float flicker = 0.7f + 0.3f * MathF.Sin(t * 9 + at.X);
            DrawCentered(batch, _halo, at, new Color(255, 210, 120) * (0.6f * flicker), 0, 0.07f);
            DrawCentered(batch, _flare, at, Color.White * flicker, 0, 0.05f);
        }
        batch.End();
    }

    string PoseOf(Fan fan)
    {
        // Pouca energia: parados, um ou outro mexendo; muita: braços para cima e pulos na batida.
        int beat = _lastBeat + (int)(fan.Phase * 4);
        float excitement = _energy + 0.25f * (fan.Phase - 0.5f) + 0.3f * _starPower;
        if (excitement < 0.3f) return CalmPoses[MathUtil.Mod((int)(fan.Phase * 10) + beat / 4, CalmPoses.Length)];
        if (excitement < 0.6f) return MathUtil.Mod(beat, 2) == 0 ? "cheer0" : "idle";
        return _beatPhase < 0.5f ? (fan.Phase < 0.5f ? "jump" : "cheer1") : "cheer0";
    }

    /// <summary>Facho de luz: estreito em cima, abrindo e sumindo para baixo (aditivo, fundo preto).</summary>
    static Texture2D MakeBeamTexture(GraphicsDevice device)
    {
        const int width = 64, height = 256;
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            float half = 0.06f + 0.94f * v;
            float along = MathF.Pow(1 - v, 0.8f) * MathF.Min(1, v * 25);
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width * 2 - 1;
                float across = MathF.Exp(-MathF.Pow(u / half, 2) * 3.5f);
                float value = along * across;
                pixels[y * width + x] = new Color(value, value, value, 1f);
            }
        }
        var texture = new Texture2D(device, width, height);
        texture.SetData(pixels);
        return texture;
    }
}
