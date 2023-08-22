using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AvaloniaLoudnessMeterX.DataModels;
using ManagedBass;
using NWaves.Signals;
using NWaves.Utils;

namespace AvaloniaLoudnessMeterX.Services;

public class BassAudioCaptureService : IDisposable, IAudioCaptureService
{
    private readonly int _device;
    private readonly int _handle;
    private readonly int _sampleFrequency;
    private byte[]? _buffer;
    private readonly Queue<double> _shortTermLUFSBuffer = new();
    private readonly Queue<double> _longTermLUFSBuffer = new();
    private double HistoricalMaxLUFS { get; set; } = double.NegativeInfinity;
    public event Action<AudioChunkData>? AudioChunkAvailable;
    
    public void StartCapture()
    {
        Bass.ChannelPlay(_handle);
    }

    private void CalculateAudioChunk(byte[] buffer)
    {
        var sampleLength = buffer.Length / 2;
        var signal = new DiscreteSignal(_sampleFrequency, sampleLength);
        using var reader = new BinaryReader(new MemoryStream(buffer));
        for (var i = 0; i < sampleLength; i++)
        {
            signal[i] = reader.ReadInt16() / 32768f;
        }

        var lufs = Scale.ToDecibel(signal.Rms());
        _shortTermLUFSBuffer.Enqueue(lufs);
        _longTermLUFSBuffer.Enqueue(lufs);
        HistoricalMaxLUFS = Math.Max(HistoricalMaxLUFS, _longTermLUFSBuffer.Max());
        if (_shortTermLUFSBuffer.Count > 15)
        {
            _shortTermLUFSBuffer.Dequeue();
        }

        if (_longTermLUFSBuffer.Count > 1000)
        {
            _longTermLUFSBuffer.Dequeue();
        }
        var shortTermAvg = _shortTermLUFSBuffer.Average();
        var longTermAvg = _longTermLUFSBuffer.Average();
        var shortTermLufs = double.IsInfinity(shortTermAvg) ? 0 : shortTermAvg;
        AudioChunkAvailable?.Invoke(new AudioChunkData(
            shortTermLufs,
            lufs,
            shortTermLufs * 0.9,
            shortTermLufs * 0.9,
            shortTermLufs * 0.9,
            _longTermLUFSBuffer.Average(),
            HistoricalMaxLUFS,
            shortTermLufs,
            shortTermLufs)
        );
    }
    public BassAudioCaptureService(RecordingDevice device, int sampleFrequency = 44100)
    {
        _device = device.Index;
        _sampleFrequency = sampleFrequency;
        Bass.RecordInit(_device);
        _handle = Bass.RecordStart(_sampleFrequency, 2, BassFlags.RecordPause, 20, Procedure);
    }

    private bool Procedure(int handle, IntPtr buffer, int length, IntPtr user)
    {
        if (_buffer is null || _buffer.Length < length)
            _buffer = new byte[length];

        Marshal.Copy(buffer, _buffer, 0, length);

        CalculateAudioChunk(_buffer);
        
        return true;
    }
    
    public void StopCapture()
    {
        Bass.ChannelStop(_handle);
    }


    public void Dispose()
    {
        Bass.CurrentRecordingDevice = _device;

        Bass.RecordFree();
    }

    public Task<List<ChannelConfigurationItem>> GetChannelConfigurationsAsync()
    {
        return Task.FromResult(new List<ChannelConfigurationItem>()
        {
            new() { LongText = "Stereo", ShortText = "Stereo", GroupName = "Mono Stereo Configuration" },
            new() { LongText = "Mono", ShortText = "Mono", GroupName = "Mono Stereo Configuration" },
            new() { LongText = "5.1 DTS - (L, R, Ls, Rs, C, LFE)", ShortText = "5.1 DTS", GroupName = "5.1 Surround" },
            new() { LongText = "5.1 DTS - (L, R, C, LFE, Ls, Rs)", ShortText = "5.1 ITU", GroupName = "5.1 Surround" },
            new() { LongText = "5.1 DTS - (L, C, R, Ls, Rs, LFE)", ShortText = "5.1 FILM", GroupName = "5.1 Surround" },
        });
    }

}