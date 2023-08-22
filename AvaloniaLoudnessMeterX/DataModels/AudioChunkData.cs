namespace AvaloniaLoudnessMeterX.DataModels;

public record class AudioChunkData(
    double IntegratedLUFS, 
    double LoudnessRange,
    double RealtimeDynamic,
    double AvgDynamic,
    double MomentaryMax,
    double ShortTermMax,
    double TruePeakMax,
    double Loudness,
    double ShortTermLUFS = 0 
    );
