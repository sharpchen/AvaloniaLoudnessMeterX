using System;
using System.Collections.Generic;
using ManagedBass;

namespace AvaloniaLoudnessMeterX.Services;

public class RecordingDevice : IDisposable
{
    private readonly string _name;
    public int Index { get; }

    public RecordingDevice(string name,int index = 0)
    {
        Index = index;
        _name = name;
    }

    public static IEnumerable<RecordingDevice> Enumerate()
    {
        for (var i = 0; Bass.RecordGetDeviceInfo(i, out var info); ++i)
            yield return new RecordingDevice(info.Name, i);
    }

    public void Dispose()
    {
        Bass.CurrentRecordingDevice = Index;
        Bass.RecordFree();
    }

    public override string ToString() => _name;
}