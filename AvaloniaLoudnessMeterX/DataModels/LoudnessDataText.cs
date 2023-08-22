using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaLoudnessMeterX.DataModels;

public partial class LoudnessDataText : ObservableObject
{
    [ObservableProperty] private string? _shortTermLUFS;
    [ObservableProperty] private string? _integratedLUFS;
    [ObservableProperty] private string? _loudnessRange;
    [ObservableProperty] private string? _realtimeDynamic;
    [ObservableProperty] private string? _avgDynamic;
    [ObservableProperty] private string? _momentaryMax;
    [ObservableProperty] private string? _shortTermMax;
    [ObservableProperty] private string? _truePeakMax;
}