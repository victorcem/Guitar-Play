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

    public ResultScreen(GuitarPlayGame game, ActivityResult result, MenuState state)
    {
        _game = game;
        _result = result;
        _state = state;
        if (result.ProgressKey is not null) game.Progress.RecordStars(result.ProgressKey, result.Stars);
    }

    public void Update(GameTime gameTime)
    {
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
            var color = i < _result.Stars ? Palette.Gold : Palette.Panel;
            _game.Text("★", new Vector2(cx + (i - 1) * 120, 250), 110, color, align: Align.Center, middle: true, shadow: true);
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
    }
}
