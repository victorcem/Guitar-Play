using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GuitarPlay.Rendering;

/// <summary>Arquivos da pasta Content (arte CC0, ver CREDITS.md), lidos direto do disco e guardados em cache.</summary>
public sealed class Assets
{
    readonly GraphicsDevice _device;
    readonly Dictionary<string, Texture2D> _textures = new();
    readonly Dictionary<string, Texture2D> _masks = new();
    readonly Dictionary<string, ObjMesh> _meshes = new();

    public Assets(GraphicsDevice device) => _device = device;

    public static string PathOf(string relative) => Path.Combine(AppContext.BaseDirectory, "Content", relative);

    /// <summary>Textura com alfa pré-multiplicado (o padrão do SpriteBatch).</summary>
    public Texture2D Texture(string relative)
    {
        if (!_textures.TryGetValue(relative, out var texture))
            _textures[relative] = texture = Texture2D.FromFile(_device, PathOf(relative), DefaultColorProcessors.PremultiplyAlpha);
        return texture;
    }

    /// <summary>Silhueta branca da textura (mesmo alfa), para pintar de qualquer cor.</summary>
    public Texture2D Mask(string relative)
    {
        if (_masks.TryGetValue(relative, out var mask)) return mask;
        var source = Texture(relative);
        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(pixels[i].A, pixels[i].A, pixels[i].A, pixels[i].A);
        _masks[relative] = mask = new Texture2D(_device, source.Width, source.Height);
        mask.SetData(pixels);
        return mask;
    }

    public ObjMesh Mesh(string relative)
    {
        if (!_meshes.TryGetValue(relative, out var mesh))
            _meshes[relative] = mesh = ObjMesh.Load(_device, PathOf(relative));
        return mesh;
    }

    public Effect Effect(string relative) => new(_device, File.ReadAllBytes(PathOf(relative)));
}
