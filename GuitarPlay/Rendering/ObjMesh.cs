using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GuitarPlay.Rendering;

/// <summary>
/// Modelo 3D lido de um .obj (só posição, normal e UV; faces viram leques de triângulos).
/// Os modelos do palco usam uma textura-paleta 32x32, então não há materiais a ler.
/// </summary>
public sealed class ObjMesh
{
    readonly VertexBuffer _vertices;

    ObjMesh(VertexBuffer vertices, BoundingBox bounds)
    {
        _vertices = vertices;
        Bounds = bounds;
    }

    public BoundingBox Bounds { get; }

    public static ObjMesh Load(GraphicsDevice device, string path)
    {
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var vertices = new List<VertexPositionNormalTexture>();
        var corners = new List<VertexPositionNormalTexture>();

        foreach (string line in File.ReadLines(path))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;
            switch (parts[0])
            {
                case "v": positions.Add(new Vector3(F(parts[1]), F(parts[2]), F(parts[3]))); break;
                case "vn": normals.Add(new Vector3(F(parts[1]), F(parts[2]), F(parts[3]))); break;
                // O .obj conta o V de baixo para cima; a textura do MonoGame, de cima para baixo.
                case "vt": uvs.Add(new Vector2(F(parts[1]), 1 - F(parts[2]))); break;
                case "f":
                    corners.Clear();
                    for (int i = 1; i < parts.Length; i++) corners.Add(Corner(parts[i]));
                    for (int i = 1; i + 1 < corners.Count; i++)
                    {
                        vertices.Add(corners[0]);
                        vertices.Add(corners[i]);
                        vertices.Add(corners[i + 1]);
                    }
                    break;
            }
        }

        var buffer = new VertexBuffer(device, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
        buffer.SetData(vertices.ToArray());
        return new ObjMesh(buffer, BoundingBox.CreateFromPoints(positions));

        VertexPositionNormalTexture Corner(string spec)
        {
            // "v/vt/vn", índices a partir de 1 (negativos contam do fim).
            var ids = spec.Split('/');
            var position = positions[Index(ids[0], positions.Count)];
            var uv = ids.Length > 1 && ids[1].Length > 0 ? uvs[Index(ids[1], uvs.Count)] : Vector2.Zero;
            var normal = ids.Length > 2 && ids[2].Length > 0 ? normals[Index(ids[2], normals.Count)] : Vector3.Up;
            return new VertexPositionNormalTexture(position, normal, uv);
        }
    }

    static int Index(string text, int count)
    {
        int i = int.Parse(text, CultureInfo.InvariantCulture);
        return i > 0 ? i - 1 : count + i;
    }

    static float F(string text) => float.Parse(text, CultureInfo.InvariantCulture);

    public void Draw(GraphicsDevice device, Effect effect)
    {
        device.SetVertexBuffer(_vertices);
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, _vertices.VertexCount / 3);
        }
    }
}
