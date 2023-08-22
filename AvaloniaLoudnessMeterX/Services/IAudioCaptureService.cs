using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaLoudnessMeterX.DataModels;

namespace AvaloniaLoudnessMeterX.Services;

public interface IAudioCaptureService
{
    Task<List<ChannelConfigurationItem>> GetChannelConfigurationsAsync();

    void StartCapture();
    void StopCapture();

    event Action<AudioChunkData> AudioChunkAvailable;
}