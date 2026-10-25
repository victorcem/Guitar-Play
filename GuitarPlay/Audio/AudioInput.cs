using System.Collections.Concurrent;
using System.Diagnostics;
using GuitarPlay.Gameplay;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace GuitarPlay.Audio;

/// <summary>
/// Captura o som de um dispositivo de entrada do Windows (Pocket Master por USB, interface de áudio
/// ou microfone) via WASAPI e entrega as notas detectadas.
/// </summary>
public sealed class AudioInput : IDisposable
{
    public readonly record struct DeviceInfo(string Id, string Name);

    // KSDATAFORMAT_SUBTYPE_IEEE_FLOAT: o formato de mixagem padrão do Windows (float 32 bits).
    static readonly Guid IeeeFloatSubFormat = new("00000003-0000-0010-8000-00aa00389b71");

    readonly MMDeviceEnumerator _enumerator = new();
    readonly WasapiCapture _capture;
    readonly NoteTracker _tracker;
    readonly Stopwatch _clock;
    readonly int _channels;
    readonly int _bytesPerSample;
    readonly bool _isFloat;
    float[] _mono = Array.Empty<float>();

    public AudioInput(string deviceId, Settings settings, Stopwatch clock)
    {
        _clock = clock;
        var device = _enumerator.GetDevice(deviceId);
        DeviceName = device.FriendlyName;
        // Modo compartilhado com buffer curto (10 ms) para baixa latência.
        _capture = new WasapiCapture(device, useEventSync: true, audioBufferMillisecondsLength: 10);

        var format = _capture.WaveFormat;
        _channels = format.Channels;
        _bytesPerSample = format.BitsPerSample / 8;
        _isFloat = format.Encoding == WaveFormatEncoding.IeeeFloat
            || (format is WaveFormatExtensible extensible && extensible.SubFormat == IeeeFloatSubFormat);
        SampleRate = format.SampleRate;

        _tracker = new NoteTracker(SampleRate, settings);
        _capture.DataAvailable += OnData;
        _capture.StartRecording();
    }

    public string DeviceName { get; }
    public int SampleRate { get; }
    public ConcurrentQueue<AudioNoteEvent> Events => _tracker.Events;
    public TunerReading Reading => _tracker.Reading;

    public static List<DeviceInfo> ListDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(d => new DeviceInfo(d.ID, d.FriendlyName))
            .ToList();
    }

    void OnData(object? sender, WaveInEventArgs e)
    {
        double arrival = _clock.Elapsed.TotalSeconds;
        int frames = e.BytesRecorded / (_bytesPerSample * _channels);
        if (_mono.Length < frames) _mono = new float[frames];

        var bytes = e.Buffer;
        for (int f = 0; f < frames; f++)
        {
            // Média dos canais: a guitarra pode vir só no esquerdo, só no direito ou nos dois.
            float sum = 0;
            for (int c = 0; c < _channels; c++) sum += ReadSample(bytes, (f * _channels + c) * _bytesPerSample);
            _mono[f] = sum / _channels;
        }
        _tracker.Process(_mono.AsSpan(0, frames), arrival);
    }

    float ReadSample(byte[] buffer, int offset) => (_isFloat, _bytesPerSample) switch
    {
        (true, 4) => BitConverter.ToSingle(buffer, offset),
        (false, 2) => BitConverter.ToInt16(buffer, offset) / 32768f,
        (false, 3) => ((buffer[offset] << 8 | buffer[offset + 1] << 16 | buffer[offset + 2] << 24) >> 8) / 8388608f,
        (false, 4) => BitConverter.ToInt32(buffer, offset) / 2147483648f,
        _ => 0f,
    };

    public void Dispose()
    {
        _capture.DataAvailable -= OnData;
        _capture.StopRecording();
        _capture.Dispose();
        _enumerator.Dispose();
    }
}
