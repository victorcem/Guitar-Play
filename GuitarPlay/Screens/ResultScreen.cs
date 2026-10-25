using GuitarPlay.Gameplay;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

public sealed class ResultScreen : IScreen
{
    readonly GuitarPlayGame _game;
    readonly ActivityResult _result;
    readonly MenuState _state;
    readonly Particles _particles = new();
    double _time;
    int _starsShown;

    static readonly Color[] ConfettiColors =
    {
        Palette.Root, Palette.Third, Palette.Fifth, Palette.Gold, Palette.BlueNote, Palette.Added,
    };

    /// <summary>Quando cada estrela aparece (uma de cada vez, como num placar).</summary>
    static double StarTime(int i) => 0.35 + 0.4 * i;

    static Vector2 StarPosition(int i) => new(GuitarPlayGame.Width / 2f + (i - 1) * 120, 250);

    public ResultScreen(GuitarPlayGame game, ActivityResult result, MenuState state)
    {
        _game = game;
        _result = result;
        _state = state;
        if (result.ProgressKey is not null) game.Progress.RecordStars(result.ProgressKey, result.Stars);
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _time += dt;
        _particles.Update(dt);
        if (_starsShown < _result.Stars && _time >= StarTime(_starsShown))
        {
            // Cada estrela entra com faíscas e uma nota subindo (Sol, Si, Ré).
            _particles.Burst(StarPosition(_starsShown), Palette.Gold, 26, 700);
            _game.Synth.Play(67 + new[] { 0, 4, 7 }[_starsShown]);
            _starsShown++;
        }
        bool passed = _result.Stars >= StarRating.ToPass;
        if (passed && _time > StarTime(_result.Stars - 1) && _time < 3.5)
            for (int i = 0; i < (int)(dt * 60) + 1; i++)
                _particles.Emit(new Vector2(_particles.Next(0, GuitarPlayGame.Width), -10),
                    new Vector2(_particles.Next(-40, 40), _particles.Next(120, 260)),
                    ConfettiColors[(int)_particles.Next(0, ConfettiColors.Length - 0.01f)], _particles.Next(2.5f, 4), _particles.Next(3, 5), gravity: 40);

        if (_game.Pressed(Keys.Enter) || _game.Pressed(Keys.Space))
            _game.ShowScreen(_state.CreateScreen(_game));
        else if (_game.Pressed(Keys.Escape))
            _game.ShowScreen(new MenuScreen(_game, _state));
    }

    public void Draw(GameTime gameTime)
    {
        _game.DrawBackground();
        _game.Primitives.Flush();
        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

        const float cx = GuitarPlayGame.Width / 2f;
        _game.Text("RESULTADO", new Vector2(cx, 50), 22, Palette.TextFaint, bold: true, align: Align.Center);
        _game.Text(_result.Title, new Vector2(cx, 80), 48, Palette.Text, bold: true, align: Align.Center);
        _game.Text(_result.Subtitle, new Vector2(cx, 140), 20, Palette.TextDim, align: Align.Center);

        for (int i = 0; i < 3; i++)
        {
            _game.Text("★", StarPosition(i), 110, Palette.Panel, align: Align.Center, middle: true, shadow: true);
            double age = _time - StarTime(i);
            if (i >= _result.Stars || age < 0) continue;
            // Entra grande e "assenta" no lugar.
            float pop = 1 + 0.8f * MathF.Max(0, 1 - (float)age * 5);
            _game.Text("★", StarPosition(i), 110 * pop, Palette.Gold, align: Align.Center, middle: true, shadow: true);
        }

        bool passed = _result.Stars >= StarRating.ToPass;
        string message = passed ? "Nível concluído!"
            : _result.Stars == StarRating.ToPass - 1 ? $"Faltou pouco: são necessárias {StarRating.ToPass}★ para avançar."
            : $"Continue praticando: são necessárias {StarRating.ToPass}★ para avançar.";
        _game.Text(message, new Vector2(cx, 330), 26, passed ? Palette.Third : Palette.Other, bold: true, align: Align.Center);
        if (passed && _result.UnlockMessage is not null)
            _game.Text(_result.UnlockMessage, new Vector2(cx, 364), 20, Palette.Gold, bold: true, align: Align.Center);

        var stats = _result.Stats.Prepend((Label: "Precisão", Value: $"{_result.Accuracy:P0}")).ToList();
        for (int i = 0; i < stats.Count; i++)
        {
            float y = 410 + i * 38;
            _game.Text(stats[i].Label, new Vector2(cx - 20, y), 22, Palette.TextDim, align: Align.Right);
            _game.Text(stats[i].Value, new Vector2(cx + 20, y), 22, Palette.Text, bold: true);
        }

        _game.Text("1★ 60%   ·   2★ 80%   ·   3★ 95%", new Vector2(cx, 620), 17, Palette.TextFaint, align: Align.Center);
        _game.Text("Enter: jogar de novo     Esc: voltar ao menu", new Vector2(cx, 660), 20, Palette.TextDim, align: Align.Center);

        _game.Batch.End();

        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        _particles.Draw(_game);
        _game.Batch.End();
    }
}
