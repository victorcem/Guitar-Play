namespace GuitarPlay.Gameplay;

/// <summary>Configurações de áudio, salvas em %AppData%\GuitarPlay\settings.json.</summary>
public sealed class Settings
{
    const string FileName = "settings.json";

    /// <summary>Dispositivo de entrada (Pocket Master, interface, microfone). Null = só teclado.</summary>
    public string? InputDeviceId { get; set; }

    /// <summary>Atraso total entre tocar e o jogo registrar, medido na calibração.</summary>
    public double LatencySeconds { get; set; } = 0.08;

    public bool Calibrated { get; set; }

    /// <summary>Volume mínimo (RMS) para considerar que há som; abaixo disso é silêncio/ruído.</summary>
    public float NoiseGate { get; set; } = 0.01f;

    public static Settings Load() => JsonStore.Load<Settings>(FileName);

    public void Save() => JsonStore.Save(FileName, this);
}
