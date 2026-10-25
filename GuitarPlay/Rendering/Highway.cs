using Microsoft.Xna.Framework;

namespace GuitarPlay.Rendering;

/// <summary>
/// Projeção da pista estilo Guitar Hero. t = 0 é a linha de batida, t = 1 o fundo da pista.
/// Cada corda é uma trilha: a 6ª fica à esquerda e a 1ª à direita.
/// </summary>
public sealed class Highway
{
    const float Depth = 3f;

    public Highway(float centerX, float strikeY, float farY, float laneSpacing)
    {
        CenterX = centerX;
        StrikeY = strikeY;
        LaneSpacing = laneSpacing;
        float farScale = Scale(1);
        VanishY = (farY - strikeY * farScale) / (1 - farScale);
    }

    public float CenterX { get; }
    public float StrikeY { get; }
    public float VanishY { get; }
    public float LaneSpacing { get; }

    /// <summary>Tamanho relativo de algo a uma distância t (1 na linha de batida).</summary>
    public static float Scale(float t) => 1f / (1f + Depth * t);

    /// <summary>Posição lateral da corda em "trilhas" a partir do centro (-2,5 a 2,5).</summary>
    public static float LaneX(int guitarString) => 6 - guitarString - 2.5f;

    public Vector2 Project(float laneX, float t)
    {
        float s = Scale(t);
        return new Vector2(CenterX + laneX * LaneSpacing * s, VanishY + (StrikeY - VanishY) * s);
    }
}
