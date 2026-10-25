using GuitarPlay.Gameplay;
using GuitarPlay.Music;
using GuitarPlay.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GuitarPlay.Screens;

public sealed class ResultScreen : IScreen
{
    /// <summary>Estrelas necessárias para passar de nível.</summary>
    public const int StarsToPass = 2;

    readonly GuitarPlayGame _game;
    readonly Judge _judge;
    readonly int _levelIndex;
    readonly int _bpmIndex;

    public ResultScreen(GuitarPlayGame game, Judge judge, int levelIndex, int bpmIndex)
    {
        _game = game;
        _judge = judge;
        _levelIndex = levelIndex;
        _bpmIndex = bpmIndex;
    }

    public void Update(GameTime gameTime)
    {
        if (_game.Pressed(Keys.Enter) || _game.Pressed(Keys.Space))
            _game.ShowScreen(new PlayScreen(_game, _levelIndex, _bpmIndex));
        else if (_game.Pressed(Keys.Escape))
            _game.ShowScreen(new MenuScreen(_game, _levelIndex, _bpmIndex));
    }

    public void Draw(GameTime gameTime)
    {
        _game.DrawBackground();
        _game.Primitives.Flush();
        _game.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

        const float cx = GuitarPlayGame.Width / 2f;
        var level = LevelFactory.World1[_levelIndex];
        _game.Text("RESULTADO", new Vector2(cx, 50), 22, Palette.TextFaint, bold: true, align: Align.Center);
        _game.Text(level.Title, new Vector2(cx, 80), 48, Palette.Text, bold: true, align: Align.Center);
        _game.Text($"{level.Subtitle} · {MenuScreen.BpmOptions[_bpmIndex]} BPM", new Vector2(cx, 140), 20,
            Palette.TextDim, align: Align.Center);

        for (int i = 0; i < 3; i++)
        {
            var color = i < _judge.Stars ? Palette.Gold : Palette.Panel;
            _game.Text("★", new Vector2(cx + (i - 1) * 120, 250), 110, color, align: Align.Center, middle: true, shadow: true);
        }

        bool passed = _judge.Stars >= StarsToPass;
        string message = passed ? "Nível concluído!"
            : _judge.Stars == StarsToPass - 1 ? $"Faltou pouco: são necessárias {StarsToPass}★ para avançar."
            : $"Continue praticando: são necessárias {StarsToPass}★ para avançar.";
        _game.Text(message, new Vector2(cx, 330), 26, passed ? Palette.Third : Palette.Other, bold: true, align: Align.Center);

        (string Label, string Value)[] stats =
        {
            ("Precisão", $"{_judge.Accuracy:P0}"),
            ("Pontos", _judge.Score.ToString("N0")),
            ("Maior combo", _judge.MaxCombo.ToString()),
            ("Perfeito / Bom / Errou", $"{_judge.Perfects} / {_judge.Goods} / {_judge.Misses}"),
        };
        for (int i = 0; i < stats.Length; i++)
        {
            float y = 400 + i * 40;
            _game.Text(stats[i].Label, new Vector2(cx - 20, y), 22, Palette.TextDim, align: Align.Right);
            _game.Text(stats[i].Value, new Vector2(cx + 20, y), 22, Palette.Text, bold: true);
        }

        _game.Text("1★ 60%   ·   2★ 80%   ·   3★ 95%", new Vector2(cx, 580), 17, Palette.TextFaint, align: Align.Center);
        _game.Text("Enter: tocar de novo     Esc: voltar ao menu", new Vector2(cx, 640), 20, Palette.TextDim, align: Align.Center);

        _game.Batch.End();
    }
}
