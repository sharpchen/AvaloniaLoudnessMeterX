using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaLoudnessMeterX.Services;
using AvaloniaLoudnessMeterX.ViewModels;

namespace AvaloniaLoudnessMeterX.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        await (DataContext as MainViewModel)?.LoadCommand.ExecuteAsync(null)!;
        base.OnLoaded(e);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var p = ChannelConfigurationButton.TranslatePoint(new Point(), MainGrid).GetValueOrDefault();
        Dispatcher.UIThread.Post(() => ChannelConfigurationPopUp.Margin = new Thickness(p.X, 0, 0,
            MainGrid.Bounds.Height - p.Y - ChannelConfigurationButton.Bounds.Height));
        Dispatcher.UIThread.Post(UpdateSize);
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        (DataContext as MainViewModel)?.ChannelConfigurationButtonPressedCommand.Execute(null);
    }
    
    private void UpdateSize()
    {
        (DataContext! as MainViewModel)!.VolumeArrowContainerHeight = VolumeArrowContainer.Bounds.Height;
        (DataContext! as MainViewModel)!.VolumeBarHeight = VolumeBar.Bounds.Height;
    }
}