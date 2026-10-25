using System.Text.Json;

namespace GuitarPlay.Gameplay;

/// <summary>Lê e grava arquivos JSON em %AppData%\GuitarPlay sem derrubar o jogo se o disco falhar.</summary>
static class JsonStore
{
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    static string PathOf(string fileName) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GuitarPlay", fileName);

    public static T Load<T>(string fileName) where T : new()
    {
        try
        {
            string path = PathOf(fileName);
            if (File.Exists(path)) return JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ?? new T();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // Arquivo corrompido ou inacessível: começa do zero em vez de travar o jogo.
        }
        return new T();
    }

    public static void Save<T>(string fileName, T value)
    {
        try
        {
            string path = PathOf(fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(value, Options));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Sem disco: o que mudou nesta sessão continua na memória.
        }
    }
}
