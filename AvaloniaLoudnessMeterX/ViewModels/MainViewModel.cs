using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvaloniaLoudnessMeterX.DataModels;
using AvaloniaLoudnessMeterX.Services;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AvaloniaLoudnessMeterX.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private double _volumeArrowContainerHeight;
    [ObservableProperty] private double _volumeBarHeight;

    [NotifyPropertyChangedFor(nameof(VolumePercentPosition))]
    [ObservableProperty]
    private double _volumeBarMaskHeight;

    [ObservableProperty] private string _boldTitle = "AVALONIA";
    [ObservableProperty] private bool _isChannelConfigurationListOpened;
    [ObservableProperty] private LoudnessDataText _loudnessText = new();
    [ObservableProperty] private string _regularTitle = "Loudness Meter".ToUpper();
    [ObservableProperty] private IAudioCaptureService? _audioCaptureService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChannelConfigurationButtonText))]
    private ChannelConfigurationItem? _selectedChannelConfiguration;

    [ObservableProperty] private string _channelConfigurationButtonText = "Select Channel";
    [ObservableProperty] private double _volumePercentPosition;

    [RelayCommand]
    private void ChannelConfigurationButtonPressed() => IsChannelConfigurationListOpened ^= true;

    [RelayCommand]
    private void ChannelConfigurationPressed(ChannelConfigurationItem config)
    {
        SelectedChannelConfiguration = config;
        ChannelConfigurationButtonText = config.ShortText!;
        IsChannelConfigurationListOpened ^= true;
    }

    [ObservableProperty]
    private ObservableCollection<ObservableGroup<string, ChannelConfigurationItem>>? _channelConfigurations;

    public MainViewModel()
    { }

    public MainViewModel(IAudioCaptureService captureService)
    {
        _audioCaptureService = captureService;
        Series = new ObservableCollection<ISeries>
        {
            new LineSeries<ObservableValue>
            {
                Values = _loudnessSequence,
                Fill = new SolidColorPaint(new SKColor(63,77,99)),
                GeometrySize = 0,
                GeometryStroke = new SolidColorPaint(new SKColor(120,152,203),3),
                Stroke = new SolidColorPaint(new SKColor(120,152,203),3)
            }
        };
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var channelConfigs = await AudioCaptureService?.GetChannelConfigurationsAsync()!;
        ChannelConfigurations =
            new ObservableGroupedCollection<string, ChannelConfigurationItem>(
                channelConfigs.GroupBy(x => x.GroupName)!);
        AudioCaptureService!.AudioChunkAvailable += UpdateLoudnessInfo;
        AudioCaptureService.StartCapture();
    }

    private void UpdateLoudnessInfo(AudioChunkData data)
    {
        if (!MainViewModelHelper.SecondsPassedTimer.IsEnabled)
            MainViewModelHelper.SecondsPassedTimer.Start();
        Dispatcher.UIThread.Post(() =>
        {
            VolumeBarMaskHeight = Math.Min(VolumeBarHeight, VolumeBarHeight / 60d * -data.Loudness);
            VolumePercentPosition = Math.Min(VolumeArrowContainerHeight,
                VolumeArrowContainerHeight / 60d * -data.ShortTermLUFS);
        });
        if (MainViewModelHelper.UpdateInfoCountDown() == 0)
            Dispatcher.UIThread.Post(() =>
            {
                LoudnessText.ShortTermLUFS = $"{data.ShortTermLUFS:F1} LUFS";
                LoudnessText.IntegratedLUFS = $"{data.IntegratedLUFS:F1} LUFS";
                LoudnessText.LoudnessRange = $"{data.LoudnessRange:F1} LU";
                LoudnessText.RealtimeDynamic = $"{data.RealtimeDynamic:F1} LU";
                LoudnessText.AvgDynamic = $"{data.AvgDynamic:F1} LU";
                LoudnessText.MomentaryMax = $"{data.MomentaryMax:F1} LUFS";
                LoudnessText.ShortTermMax = $"{data.ShortTermMax:F1} LUFS";
                LoudnessText.TruePeakMax = $"{data.TruePeakMax:F1} dB";
                _loudnessSequence.Flow(new(data.ShortTermLUFS + 60 < 0 ? 0 : data.ShortTermLUFS + 60), countLimit: MainViewModelHelper.ChartPointCountRange);
            });
    }

    private readonly ObservableCollection<ObservableValue> _loudnessSequence = new();

    public ObservableCollection<ISeries>? Series { get; set; }

    public Axis[] YAxes { get; } =
    {
        new()
        {
            IsVisible = false,
            MinLimit = 0,
            MaxLimit = 60
            // IsInverted = true
        }
    };

    public Axis[] XAxes { get; } =
    {
        new()
        {
            IsVisible = true,
            Labeler = _ => MainViewModelHelper.PassedTimeLabel
        }
    };
}

internal static class ObservableCollectionExtension
{
    internal static void Flow(this ObservableCollection<ObservableValue> collection, ObservableValue value, uint countLimit)
    {
        if (collection.Count < countLimit)
            collection.Add(value);
        else if (collection.Count >= countLimit)
        {
            collection.RemoveAt(0);
            collection.Add(value);
        }
    }
}

internal static class MainViewModelHelper
{
    private const byte CountDownRange = 5;
    private static byte _countDown;
    internal const uint ChartPointCountRange = 100;
    private static uint SecondsPassed;

    internal static readonly DispatcherTimer SecondsPassedTimer;

    static MainViewModelHelper()
    {
        SecondsPassedTimer = new()
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        SecondsPassedTimer.Tick += (_, _) => SecondsPassed++;
    }

    internal static string PassedTimeLabel
    {
        get
        {
            var secLeft = SecondsPassed % 60;
            var minTotal = SecondsPassed / 60;
            var minLeft = minTotal % 60;
            var hourTotal = minTotal / 60;
            return (secLeft, minTotal, hourTotal) switch
            {
                { secLeft: > 0, minTotal: 0 } => $"00:{SecondsPassed:00}",
                { minTotal: > 0, hourTotal: 0 } => $"{minLeft:00}:{secLeft:00}",
                { hourTotal: > 0 } => $"{hourTotal:00}:{minLeft:00}:{secLeft:00}",
                _ => string.Empty
            };
        }
    }

    internal static byte UpdateInfoCountDown()
    {
        _countDown = (byte)((_countDown + 1) % CountDownRange);
        return _countDown;
    }
}