using System;
using System.Threading;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using static System.Double;

namespace AvaloniaLoudnessMeterX;

public partial class AnimatedPopup : ContentControl
{
    private int TickTotal => (int)(AnimationDuration / _animationFramerate.TotalSeconds);
    private readonly TimeSpan _animationFramerate = TimeSpan.FromSeconds(1 / 60d);
    private readonly DispatcherTimer _animationTimer = new();
    private readonly Timer _autoSizingTimer;
    private Size _desiredSize;
    private int _currentTick;
    private bool _isFirstAnimation = true;
    private double _animationDuration = 0.17;
    private bool IsOpened => _currentTick >= TickTotal;
    
    private bool IsOpening { get; set; }

    private bool _isSizeFound;
    private readonly Control _closingBorder;
    private double _closingBorderMaximumOpacity = 0.3;
    private bool _opened;

    public static readonly DirectProperty<AnimatedPopup, bool> OpenedProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, bool>(
        nameof(Opened), o => o.Opened, (o, v) => o.Opened = v);

    public bool Opened
    {
        get => _opened;
        set
        {
            SetAndRaise(OpenedProperty, ref _opened, value);
            Close();
        }
    }

    private static readonly DirectProperty<AnimatedPopup, double> ClosingBorderOpacityProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, double>(
        nameof(ClosingBorderMaximumOpacity), o => o.ClosingBorderMaximumOpacity, (o, v) => o.ClosingBorderMaximumOpacity = v);
    public double ClosingBorderMaximumOpacity
    {
        get => _closingBorderMaximumOpacity;
        set => SetAndRaise(ClosingBorderOpacityProperty, ref _closingBorderMaximumOpacity, value);
    }
    private static readonly DirectProperty<AnimatedPopup, double> AnimationDurationProperty =
        AvaloniaProperty.RegisterDirect<AnimatedPopup, double>(
            nameof(AnimationDuration), o => o.AnimationDuration, (o, v) => o.AnimationDuration = v);

    public double AnimationDuration
    {
        get => _animationDuration;
        set => SetAndRaise(AnimationDurationProperty, ref _animationDuration, value);
    }
    public AnimatedPopup()
    {
        _closingBorder = new Border
        {
            IsVisible = false,
            Background = Brushes.Black,
            ZIndex = ZIndex + 1,
            Opacity = 0
        };
        _closingBorder.PointerPressed += (_, _) => Close();
        _animationTimer.Interval = _animationFramerate;
        _animationTimer.Tick += (_, _) => AnimateInTick();
        _autoSizingTimer = new Timer(_ =>
        {
            if (_isSizeFound)
                return;

            _isSizeFound = true;
            Dispatcher.UIThread.Post(() =>
            {
                UpdateSize();
                Animate();
            });
        });
    }

    private void UpdateSize() => _desiredSize = DesiredSize - Margin;

    [RelayCommand]
    public void Open()
    {
        ToggleClosingBorder(expand: true);
        IsOpening = true;
        Animate();
    }
    [RelayCommand]
    public void Close()
    {
        IsOpening = false;
        UpdateSize();
        Animate();
    }

    private void AnimateInTick()
    {
        if (_isFirstAnimation)
        {
            _isFirstAnimation = false;
            return;
        }

        if (EndOfClosingAnimationReached || EndOfOpeningAnimationReached)
        {
            _animationTimer.Stop();
            AfterAnimation();
            if (EndOfClosingAnimationReached)
                ToggleClosingBorder(expand: false);
            return;
        }

        _currentTick += IsOpening ? 1 : -1;
        var animatedPercentage = (double)_currentTick / TickTotal;
        var easingAnimation = new QuadraticEaseIn();
        Dispatcher.UIThread.Post(() =>
        {
            Width = _desiredSize.Width * easingAnimation.Ease(animatedPercentage);
            Height = _desiredSize.Height * easingAnimation.Ease(animatedPercentage);
            _closingBorder.Opacity = ClosingBorderMaximumOpacity * easingAnimation.Ease(animatedPercentage);
        });
    }

    private bool EndOfOpeningAnimationReached => IsOpening && IsOpened;
    private bool EndOfClosingAnimationReached => !IsOpening && _currentTick == 0;
    private void Animate()
    {
        if (!_isSizeFound)
            return;
        _animationTimer.Start();
    }
    public override void Render(DrawingContext context)
    {
        if (!_isSizeFound)
            _autoSizingTimer.Change(10, int.MaxValue);
        base.Render(context);
    }

    private void ToggleClosingBorder(bool expand)
    {
        if (expand && Parent is Grid g)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _closingBorder.IsVisible = true;
                g.Children.Insert(0, _closingBorder);
                if (g.RowDefinitions.Count > 0)
                    _closingBorder.SetValue(Grid.RowSpanProperty, g.RowDefinitions.Count);
                if (g.ColumnDefinitions.Count > 0)
                    _closingBorder.SetValue(Grid.RowSpanProperty, g.ColumnDefinitions.Count);
            });
        }
        else if (Parent is Grid grid && grid.Children.Contains(_closingBorder))
            Dispatcher.UIThread.Post(() => grid.Children.Remove(_closingBorder));
    }
    private void AfterAnimation()
    {
        if (IsOpening)
            Width = Height = NaN;
        else
            Width = Height = 0;
    }
}