using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GuitarPlay.Rendering;

/// <summary>
/// Banda no palco, cada um com um estilo: guitarrista punk rock, vocalista de jazz, baixista de reggae
/// e baterista de hip-hop. Os músicos foram desenhados no estilo Kenney Toon Characters (CC0, ver
/// CREDITS.md) e exportados em peças (cabeça com 3 expressões, tronco, braço, mão e perna); aqui as peças
/// são montadas como na pose original e as juntas são animadas. A banda reage ao jogo: o guitarrista
/// palheta a cada acerto e se assusta no erro, todos se animam com o combo e entram em "modo show" no x4.
/// </summary>
public sealed class Band
{
    // ── Montagem (unidades do desenho da Kenney, a partir da origem do tronco) ──
    const float PixelsPerUnit = 3; // as peças foram exportadas a 3 px por unidade
    static readonly Vector2 HeadAt = new(1.85f, -27.85f);
    static readonly Vector2 NeckPivot = new(0, 6);          // a cabeça gira em torno do pescoço
    static readonly Vector2 ShoulderAt = new(17.75f, -16.25f);
    static readonly Vector2 ArmToWrist = new(8.6f, 22.1f);  // braço em repouso: do ombro ao punho
    static readonly Vector2 LegAt = new(8, 2.9f);
    const float FootY = 17;        // do tronco até a sola
    const float FigureHeight = 86; // da sola ao topo da cabeça
    static readonly float ArmReach = ArmToWrist.Length();
    static readonly float ArmRest = MathF.Atan2(ArmToWrist.Y, ArmToWrist.X);

    // Molduras das peças exportadas: canto superior esquerdo em unidades (o pivô fica na origem).
    static readonly Vector2 HeadFrame = new(-42, -70), BodyFrame = new(-34, -36), ArmFrame = new(-10, -10),
        HandFrame = new(-16, -18), LegFrame = new(-12, -10);

    /// <summary>Altura de um músico no mundo 3D do palco (maior que o real, para aparecer bem).</summary>
    const float WorldHeight = 6.2f;

    // Posições no palco 3D (x: esquerda/direita, z: fundo/frente). A pista ocupa o meio da tela, então a
    // banda fica nas laterais: guitarra e voz à esquerda, baixo à direita (a faixa entre a pista e o painel
    // só cabe um) e a bateria num praticável alto no fundo à esquerda, atrás da guitarra e da voz.
    static readonly Vector3 DrummerSpot = new(-15.2f, 10.2f, -4);
    static readonly Vector3 GuitaristSpot = new(-15.8f, 2, 6);
    static readonly Vector3 BassistSpot = new(10.4f, 2, 6);
    static readonly Vector3 VocalistSpot = new(-11.3f, 2, 8.6f);

    static readonly Color Outline = new(30, 22, 32);

    sealed class Musician
    {
        public required Texture2D Head, HeadFocus, HeadShock, Body, Arm, Hand;
        public Texture2D? Leg;
    }

    readonly Musician _guitarist, _bassist, _drummer, _vocalist;
    readonly Texture2D _disc, _pixel;
    float _strum, _shock, _jump = 99, _crash, _energy, _starPower, _beatPhase, _pulse;
    int _lastBeat = int.MinValue;
    double _time;

    public Band(GraphicsDevice device, Assets assets)
    {
        Musician Load(string name, bool legs = true)
        {
            Texture2D Part(string part) => assets.Texture($"Band/{name}/{part}.png");
            return new Musician
            {
                Head = Part("head"), HeadFocus = Part("headFocus"), HeadShock = Part("headShock"),
                Body = Part("body"), Arm = Part("arm"), Hand = Part("hand"), Leg = legs ? Part("leg") : null,
            };
        }
        _guitarist = Load("guitarist");
        _vocalist = Load("vocalist", legs: false); // as pernas ficam dentro do vestido longo
        _bassist = Load("bassist");
        _drummer = Load("drummer");
        _disc = MakeDisc(device, 64);
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Reset()
    {
        _strum = _shock = _crash = 0;
        _jump = 99;
        _lastBeat = int.MinValue;
    }

    /// <summary>Nota acertada: o guitarrista punk desce a palheta com tudo.</summary>
    public void OnHit() => _strum = 1;

    /// <summary>Nota perdida: o guitarrista faz careta de susto e dá um tranco para trás.</summary>
    public void OnMiss() => _shock = 1;

    /// <summary>Marco de combo: a banda inteira pula e o baterista ataca o prato.</summary>
    public void OnMilestone()
    {
        _jump = 0;
        _crash = 1;
    }

    public void Update(float dt, double time, int beat, float beatPhase, float energy, bool starPower)
    {
        _time = time;
        _beatPhase = beatPhase;
        _pulse = MathF.Exp(-beatPhase * 5);
        _energy = Math.Clamp(energy, 0, 1);
        _starPower += ((starPower ? 1 : 0) - _starPower) * MathF.Min(1, dt * 3);
        _strum = MathF.Max(0, _strum - dt * 6);
        _shock = MathF.Max(0, _shock - dt * 1.8f);
        _crash = MathF.Max(0, _crash - dt * 2.5f);
        _jump += dt;

        if (beat == _lastBeat) return;
        _lastBeat = beat;
        // Prato no começo do compasso quando o show esquenta; no x4, em toda batida, e a banda pula.
        if (starPower || (energy > 0.6f && MathUtil.Mod(beat, 4) == 0)) _crash = 1;
        if (starPower && MathUtil.Mod(beat, 2) == 0) _jump = 0;
    }

    /// <summary>Altura do pulo (em unidades) de um pulo que começou há <paramref name="age"/> segundos.</summary>
    static float JumpHeight(float age) => age < 0.42f ? 14 * MathF.Sin(MathF.PI * age / 0.42f) : 0;

    /// <summary>Batida contínua (inteiro + fração), para movimentos que duram mais de um tempo.</summary>
    float BeatClock => _lastBeat + _beatPhase;

    /// <param name="project">Projeção do palco: ponto 3D → tela e pixels por unidade naquela distância.</param>
    /// <param name="light">Cor da luz do palco agora (as peças ficam tingidas por ela).</param>
    public void Draw(SpriteBatch batch, Func<Vector3, (Vector2 Screen, float PixelsPerUnit)> project, Color light)
    {
        var tint = Color.Lerp(light, Color.White, 0.7f) * (0.88f + 0.16f * _pulse);
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        // Do fundo para a frente.
        DrawHipHopDrummer(batch, project(DrummerSpot), tint);
        DrawPunkGuitarist(batch, project(GuitaristSpot), tint);
        DrawReggaeBassist(batch, project(BassistSpot), tint);
        DrawJazzVocalist(batch, project(VocalistSpot), tint);
        batch.End();
    }

    // ───────────────────────── Guitarrista: punk rock ─────────────────────────

    void DrawPunkGuitarist(SpriteBatch batch, (Vector2 Screen, float PixelsPerUnit) feet, Color tint)
    {
        var m = _guitarist;
        // Pogo: um pulinho em toda batida, cada vez mais alto com o combo.
        float pogo = (1.5f + 8 * _energy) * MathF.Max(0, MathF.Sin(MathF.PI * _beatPhase));
        float lean = (0.06f + 0.1f * _energy) * (1 - _starPower) - 0.14f * _starPower + 0.14f * _shock;
        var rig = RigAt(feet, pogo + JumpHeight(_jump), lean);

        // No x4, chute de perna no começo do compasso.
        float kick = _starPower * (MathUtil.Mod(_lastBeat, 4) == 0 ? 0.55f * MathF.Exp(-_beatPhase * 4) : 0);
        DrawLegs(batch, m, rig, 2 + 3 * _energy, 0.06f * _energy, kick, tint);
        DrawBody(batch, m, rig, tint);

        // Guitarra preta com escudo branco e adesivos; o braço sobe na "pose de rock" do x4.
        float neck = -0.42f - 0.55f * _starPower + 0.05f * MathF.Sin((float)_time * 2.3f);
        var anchor = new Vector2(-1, 3);
        DrawGuitar(batch, rig, anchor, neck, tint, isBass: false, body: new Color(32, 32, 38), guard: new Color(245, 240, 230),
            stickers: new[] { new Color(255, 85, 168), new Color(255, 215, 60) });

        // Palhetada para baixo a cada acerto (a mão passa pelas cordas de cima para baixo).
        float strumY = 5 - 11 * _strum * _strum * _strum;
        DrawArm(batch, m, rig, false, anchor + Rotate(new Vector2(-2, strumY), neck), tint);
        DrawArm(batch, m, rig, true, anchor + Rotate(new Vector2(38, -3 + 1.5f * MathF.Sin((float)_time * 6)), neck), tint);

        // Headbang frenético com o combo; o susto joga a cabeça para trás.
        float bang = MathF.Sin(_beatPhase * MathHelper.TwoPi) * (0.05f + 0.32f * _energy * _energy + 0.08f * _starPower);
        var face = _shock > 0.05f ? m.HeadShock : _energy > 0.5f || _starPower > 0.5f ? m.HeadFocus : m.Head;
        DrawHead(batch, face, rig, lean + bang - 0.25f * _shock, tint);
    }

    // ───────────────────────── Vocalista: jazz ─────────────────────────

    void DrawJazzVocalist(SpriteBatch batch, (Vector2 Screen, float PixelsPerUnit) feet, Color tint)
    {
        var m = _vocalist;
        // Balanço suave de swing, um ciclo a cada dois tempos.
        float swing = MathF.Sin(BeatClock * MathF.PI);
        float lean = 0.06f * swing;
        var rig = RigAt(feet, 0.8f * MathF.Abs(swing) + JumpHeight(_jump) * 0.5f, lean, sideways: 2 * swing);

        DrawMicStand(batch, rig, tint);
        DrawBody(batch, m, rig, tint);

        // Mão direita no microfone; a esquerda estala os dedos nos tempos 2 e 4 e abre o braço com o combo.
        bool offBeat = MathUtil.Mod(_lastBeat, 2) == 1;
        float snap = offBeat ? MathF.Exp(-_beatPhase * 9) : 0;
        var gesture = Vector2.Lerp(new Vector2(-24, 4), new Vector2(-34, -14), _energy);
        gesture = Vector2.Lerp(gesture, new Vector2(-20, -50), _starPower) + new Vector2(-3, -7) * snap;
        DrawArm(batch, m, rig, true, new Vector2(12, -30), tint);
        DrawArm(batch, m, rig, false, gesture, tint);

        float tilt = 0.1f * swing + 0.06f * _starPower;
        DrawHead(batch, _starPower > 0.5f ? m.HeadFocus : m.Head, rig, lean + tilt, tint);
        // Microfone retrô na frente da boca (por cima da mão).
        var mic = rig.Point(new Vector2(14, -34));
        Ellipse(batch, mic, new Vector2(8, 10.5f) * rig.Scale, Outline, rig.Lean);
        Ellipse(batch, mic, new Vector2(6, 8.5f) * rig.Scale, Mul(new Color(214, 216, 226), tint), rig.Lean);
        BarAt(batch, mic + new Vector2(-2.2f, -1) * rig.Scale, mic + new Vector2(2.2f, -1) * rig.Scale, 0.8f * rig.Scale, Outline);
    }

    void DrawMicStand(SpriteBatch batch, Rig rig, Color tint)
    {
        var metal = Mul(new Color(185, 185, 200), tint);
        Bar(batch, rig, new Vector2(14, FootY), new Vector2(14, -30), 1.6f, metal);
        Bar(batch, rig, new Vector2(7, FootY), new Vector2(21, FootY), 1.8f, metal);
    }

    // ───────────────────────── Baixista: reggae ─────────────────────────

    void DrawReggaeBassist(SpriteBatch batch, (Vector2 Screen, float PixelsPerUnit) feet, Color tint)
    {
        var m = _bassist;
        // "Skank" do reggae: o corpo sobe no contratempo (no "e" de cada tempo), relaxado.
        float skank = (1.2f + 3.5f * _energy) * MathF.Max(0, MathF.Sin(MathHelper.TwoPi * (_beatPhase - 0.25f)));
        float sway = MathF.Sin(BeatClock * MathF.PI / 2);
        float lean = -0.05f + 0.04f * sway - 0.08f * _starPower;
        var rig = RigAt(feet, skank + JumpHeight(_jump - 0.1f), lean, sideways: 1.6f * sway);

        DrawLegs(batch, m, rig, 2, 0.03f * sway, 0, tint);
        DrawBody(batch, m, rig, tint);

        float neck = -0.26f - 0.5f * _starPower + 0.04f * sway;
        var anchor = new Vector2(1, 4);
        DrawGuitar(batch, rig, anchor, neck, tint, isBass: true, body: new Color(242, 169, 59), guard: new Color(47, 158, 84),
            stickers: Array.Empty<Color>());
        // Puxa a corda no tempo, com o som longo do reggae.
        float pluck = MathF.Exp(-_beatPhase * 4);
        DrawArm(batch, m, rig, false, anchor + Rotate(new Vector2(-1, -1 - 3.5f * pluck), neck), tint);
        DrawArm(batch, m, rig, true, anchor + Rotate(new Vector2(48, -3 + MathF.Sin(BeatClock * MathF.PI)), neck), tint);

        float nod = 0.07f * MathF.Max(0, MathF.Sin(MathHelper.TwoPi * (_beatPhase - 0.25f))) * (0.6f + _energy);
        DrawHead(batch, _starPower > 0.5f ? m.HeadFocus : m.Head, rig, lean + nod, tint);
    }

    // ───────────────────────── Baterista: hip-hop ─────────────────────────

    void DrawHipHopDrummer(SpriteBatch batch, (Vector2 Screen, float PixelsPerUnit) seat, Color tint)
    {
        var m = _drummer;
        // Boom-bap: o aceno pesado e "atrasado" cai na caixa (tempos 2 e 4).
        bool snareBeat = MathUtil.Mod(_lastBeat, 2) == 1;
        float laid = MathUtilF.Wrap(_beatPhase - 0.08f, 1);
        float nod = MathF.Exp(-laid * 5) * (snareBeat ? 0.14f + 0.2f * _energy : 0.05f);
        // Sentada atrás do kit: o tronco fica mais baixo e as pernas escondidas.
        var rig = RigAt(seat, -6 + JumpHeight(_jump) * 0.4f, -0.04f);

        DrawRiser(batch, seat, tint);
        DrawBody(batch, m, rig, tint);
        DrawHead(batch, _energy > 0.7f || _starPower > 0.5f ? m.HeadFocus : m.Head, rig, -0.04f + nod, tint);
        DrawKit(batch, rig, tint);

        // Chimbal em colcheias com swing (a segunda colcheia atrasa); caixa forte nos tempos 2 e 4.
        float eighth = _beatPhase < 2 / 3f ? _beatPhase * 1.5f : (_beatPhase - 2 / 3f) * 3;
        float rightLift = 1 - MathF.Exp(-eighth * 7);
        float leftLift = snareBeat ? 1 - MathF.Exp(-_beatPhase * 7) : 0.9f;
        var rightTarget = _crash > 0.3f ? new Vector2(30, -26) : new Vector2(-34, -10);
        var leftTarget = new Vector2(-18, 0);
        DrawArm(batch, m, rig, true, rightTarget + new Vector2(6, -4 - 9 * rightLift), tint, stickTip: rightTarget);
        DrawArm(batch, m, rig, false, leftTarget + new Vector2(8, -3 - 11 * leftLift), tint, stickTip: leftTarget);
    }

    /// <summary>Praticável da bateria: caixa escura com uma fita de LED na borda, que pisca na batida.</summary>
    void DrawRiser(SpriteBatch batch, (Vector2 Screen, float PixelsPerUnit) seat, Color tint)
    {
        float scale = WorldHeight * seat.PixelsPerUnit / FigureHeight;
        var top = seat.Screen + new Vector2(0, 8 * scale);
        var size = new Vector2(96, 70) * scale;
        batch.Draw(_pixel, new Rectangle((int)(top.X - size.X / 2), (int)top.Y, (int)size.X, (int)size.Y), new Color(18, 14, 28));
        batch.Draw(_pixel, new Rectangle((int)(top.X - size.X / 2), (int)top.Y, (int)size.X, (int)MathF.Max(2, 2.5f * scale)),
            Color.Lerp(tint, Palette.Gold, 0.5f) * (0.6f + 0.4f * _pulse));
    }

    void DrawKit(SpriteBatch batch, Rig rig, Color tint)
    {
        // Kit preto com ferragens douradas, bem hip-hop.
        var shell = Mul(new Color(32, 32, 40), tint);
        var head = Mul(new Color(238, 233, 222), tint);
        var metal = Mul(new Color(172, 172, 188), tint);
        var gold = Mul(new Color(246, 201, 69), tint);
        float kick = 1 + 0.05f * _pulse;

        Bar(batch, rig, new Vector2(-34, -10), new Vector2(-34, 22), 1.3f, metal);
        Bar(batch, rig, new Vector2(30, -26), new Vector2(30, 22), 1.3f, metal);
        var drum = rig.Point(new Vector2(0, 10));
        Ellipse(batch, drum, new Vector2(44, 44) * kick * rig.Scale, Outline, 0);
        Ellipse(batch, drum, new Vector2(40, 40) * kick * rig.Scale, gold, 0);
        Ellipse(batch, drum, new Vector2(35, 35) * kick * rig.Scale, shell, 0);
        Ellipse(batch, drum, new Vector2(15, 15) * kick * rig.Scale, gold, 0);
        Drum(batch, rig, new Vector2(-9, -8), 15, 6, shell, head);
        Drum(batch, rig, new Vector2(9, -8), 16, 6, shell, head);
        Drum(batch, rig, new Vector2(-18, 0), 20, 6, metal, head);
        Drum(batch, rig, new Vector2(25, 4), 20, 10, shell, head);
        Ellipse(batch, rig.Point(new Vector2(-34, -10)), new Vector2(25, 5) * rig.Scale, Outline, 0);
        Ellipse(batch, rig.Point(new Vector2(-34, -10)), new Vector2(23, 3.6f) * rig.Scale, gold, 0);
        float swing = -0.22f + 0.35f * _crash * MathF.Sin((float)_time * 30);
        Ellipse(batch, rig.Point(new Vector2(30, -26)), new Vector2(31, 6) * rig.Scale, Outline, swing);
        Ellipse(batch, rig.Point(new Vector2(30, -26)), new Vector2(29, 4.2f) * rig.Scale, Color.Lerp(gold, Color.White, 0.5f * _crash), swing);
    }

    void Drum(SpriteBatch batch, Rig rig, Vector2 top, float width, float depth, Color shell, Color head)
    {
        Ellipse(batch, rig.Point(top + new Vector2(0, depth / 2)), new Vector2(width + 2, depth + 6) * rig.Scale, Outline, 0);
        Ellipse(batch, rig.Point(top + new Vector2(0, depth / 2)), new Vector2(width, depth + 4.5f) * rig.Scale, shell, 0);
        Ellipse(batch, rig.Point(top), new Vector2(width, 5.5f) * rig.Scale, head, 0);
    }

    // ───────────────────────── Montagem do boneco ─────────────────────────

    /// <summary>Converte coordenadas do boneco (unidades, a partir do tronco) para a tela.</summary>
    readonly struct Rig
    {
        public readonly Vector2 Origin;
        public readonly float Scale, Lean;

        public Rig(Vector2 origin, float scale, float lean)
        {
            Origin = origin;
            Scale = scale;
            Lean = lean;
        }

        /// <summary>Ponto do corpo (gira junto com a inclinação do tronco).</summary>
        public Vector2 Point(Vector2 local) => Origin + Rotate(local, Lean) * Scale;

        /// <summary>Ponto das pernas (não inclina: os pés ficam no chão).</summary>
        public Vector2 Floor(Vector2 local) => Origin + local * Scale;
    }

    static Rig RigAt((Vector2 Screen, float PixelsPerUnit) feet, float lift, float lean, float sideways = 0)
    {
        float scale = WorldHeight * feet.PixelsPerUnit / FigureHeight;
        return new Rig(feet.Screen + new Vector2(sideways, -(FootY + lift)) * scale, scale, lean);
    }

    static Vector2 Rotate(Vector2 v, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    /// <summary>Desenha uma peça exportada com o pivô na posição dada (espelhada para o lado esquerdo).</summary>
    static void DrawPart(SpriteBatch batch, Texture2D texture, Vector2 frameTopLeft, Vector2 pivotInPart, Vector2 at,
        float rotation, Rig rig, Color tint, bool mirror = false)
    {
        var origin = (pivotInPart - frameTopLeft) * PixelsPerUnit;
        if (mirror) origin.X = texture.Width - origin.X;
        batch.Draw(texture, at, null, tint, rotation, origin, rig.Scale / PixelsPerUnit,
            mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
    }

    static void DrawBody(SpriteBatch batch, Musician m, Rig rig, Color tint) =>
        DrawPart(batch, m.Body, BodyFrame, Vector2.Zero, rig.Point(Vector2.Zero), rig.Lean, rig, tint);

    static void DrawHead(SpriteBatch batch, Texture2D face, Rig rig, float angle, Color tint) =>
        DrawPart(batch, face, HeadFrame, NeckPivot, rig.Point(HeadAt + NeckPivot), angle, rig, tint);

    static void DrawLegs(SpriteBatch batch, Musician m, Rig rig, float spread, float splay, float kick, Color tint)
    {
        if (m.Leg is null) return;
        DrawPart(batch, m.Leg, LegFrame, Vector2.Zero, rig.Floor(new Vector2(-LegAt.X - spread, LegAt.Y)), splay + kick, rig, tint, mirror: true);
        DrawPart(batch, m.Leg, LegFrame, Vector2.Zero, rig.Floor(new Vector2(LegAt.X + spread, LegAt.Y)), -splay, rig, tint);
    }

    /// <summary>"IK" simples: o braço gira do ombro em direção à mão; a mão vai no punho.</summary>
    void DrawArm(SpriteBatch batch, Musician m, Rig rig, bool rightSide, Vector2 handTarget, Color tint, Vector2? stickTip = null)
    {
        var shoulder = new Vector2(rightSide ? ShoulderAt.X : -ShoulderAt.X, ShoulderAt.Y);
        var direction = handTarget - shoulder;
        float distance = MathF.Max(0.01f, direction.Length());
        var wrist = shoulder + direction / distance * MathF.Min(distance, ArmReach);
        float pointing = MathF.Atan2(direction.Y, direction.X);
        float rest = rightSide ? ArmRest : MathF.PI - ArmRest;
        float rotation = pointing - rest + rig.Lean;

        DrawPart(batch, m.Arm, ArmFrame, Vector2.Zero, rig.Point(shoulder), rotation, rig, tint, mirror: !rightSide);
        if (stickTip is { } tip)
            BarAt(batch, rig.Point(wrist), rig.Point(tip), 1.5f * rig.Scale, Mul(new Color(236, 214, 168), tint));
        DrawPart(batch, m.Hand, HandFrame, Vector2.Zero, rig.Point(wrist), rotation, rig, tint, mirror: !rightSide);
    }

    void DrawGuitar(SpriteBatch batch, Rig rig, Vector2 anchor, float angle, Color tint, bool isBass, Color body, Color guard, Color[] stickers)
    {
        var wood = Mul(new Color(150, 95, 50), tint);
        var metal = Mul(new Color(212, 212, 222), tint);
        float neckEnd = isBass ? 66 : 52;
        Vector2 At(float x, float y) => rig.Point(anchor + Rotate(new Vector2(x, y), angle));
        float rotation = rig.Lean + angle;
        var lower = isBass ? new Vector2(21, 18) : new Vector2(24, 20);
        var upper = isBass ? new Vector2(15, 13.5f) : new Vector2(18, 15);

        // Contorno escuro primeiro (formas um pouco maiores), depois as cores — mesmo acabamento das peças.
        BarAt(batch, At(9, 0), At(neckEnd + 8, 0), 6 * rig.Scale, Outline);
        Ellipse(batch, At(-4, 2), (lower + new Vector2(2)) * rig.Scale, Outline, rotation);
        Ellipse(batch, At(6, 0), (upper + new Vector2(2)) * rig.Scale, Outline, rotation);

        BarAt(batch, At(9, 0), At(neckEnd, 0), 3.6f * rig.Scale, wood);
        BarAt(batch, At(neckEnd, 0), At(neckEnd + 7, 0), 5.4f * rig.Scale, Mul(new Color(40, 30, 35), tint));
        Ellipse(batch, At(-4, 2), lower * rig.Scale, Mul(body, tint), rotation);
        Ellipse(batch, At(6, 0), upper * rig.Scale, Mul(body, tint), rotation);
        // Sombra em meia-lua do lado direito, como nas peças.
        Ellipse(batch, At(-1, 4), lower * new Vector2(0.55f, 0.8f) * rig.Scale, Mul(Darken(body), tint) * 0.6f, rotation);
        Ellipse(batch, At(-1, 4.5f), new Vector2(11, 6.5f) * rig.Scale, Mul(guard, tint), rotation);
        for (int i = 0; i < stickers.Length; i++)
            Ellipse(batch, At(-8 + i * 12, -5 + i * 3), new Vector2(4, 4) * rig.Scale, Mul(stickers[i], tint), rotation);
        BarAt(batch, At(-2, -4), At(-2, 4), 1.8f * rig.Scale, metal);
        BarAt(batch, At(3.5f, -4), At(3.5f, 4), 1.8f * rig.Scale, metal);
        BarAt(batch, At(-8, -3.5f), At(-8, 3.5f), 1.3f * rig.Scale, Outline);
        // Cordas brilham quando tocam.
        float shine = 0.35f + 0.5f * (isBass ? _pulse : _strum);
        for (int s = 0; s < 4; s++)
        {
            float y = -1.4f + 0.9f * s;
            BarAt(batch, At(-8, y), At(neckEnd + 1, y), MathF.Max(1, 0.35f * rig.Scale), Color.White * shine);
        }
    }

    static Color Darken(Color c) => new(c.R * 3 / 4, c.G * 3 / 4, c.B * 3 / 4, c.A);

    /// <summary>Elipse cheia centrada num ponto da tela (tamanho em pixels de tela).</summary>
    void Ellipse(SpriteBatch batch, Vector2 center, Vector2 size, Color color, float rotation) =>
        batch.Draw(_disc, center, null, color, rotation, new Vector2(_disc.Width / 2f), size / _disc.Width, SpriteEffects.None, 0);

    /// <summary>Barra reta entre dois pontos do boneco (espessura em unidades).</summary>
    void Bar(SpriteBatch batch, Rig rig, Vector2 from, Vector2 to, float thickness, Color color) =>
        BarAt(batch, rig.Point(from), rig.Point(to), thickness * rig.Scale, color);

    /// <summary>Barra reta entre dois pontos da tela, com pontas arredondadas.</summary>
    void BarAt(SpriteBatch batch, Vector2 a, Vector2 b, float thickness, Color color)
    {
        var axis = b - a;
        thickness = MathF.Max(1, thickness);
        batch.Draw(_pixel, a, null, color, MathF.Atan2(axis.Y, axis.X), new Vector2(0, 0.5f),
            new Vector2(axis.Length(), thickness), SpriteEffects.None, 0);
        if (thickness < 3) return;
        Ellipse(batch, a, new Vector2(thickness), color, 0);
        Ellipse(batch, b, new Vector2(thickness), color, 0);
    }

    /// <summary>Multiplica a cor pela luz do palco (tingimento simples).</summary>
    static Color Mul(Color color, Color light) =>
        new(color.R * light.R / 255, color.G * light.G / 255, color.B * light.B / 255, color.A);

    static Texture2D MakeDisc(GraphicsDevice device, int size)
    {
        var pixels = new Color[size * size];
        float r = size / 2f - 1;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f));
            pixels[y * size + x] = Color.White * Math.Clamp(r - d, 0, 1);
        }
        var texture = new Texture2D(device, size, size);
        texture.SetData(pixels);
        return texture;
    }
}
