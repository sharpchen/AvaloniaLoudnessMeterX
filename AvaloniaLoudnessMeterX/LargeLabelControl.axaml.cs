using Avalonia;
using Avalonia.Controls.Primitives;

namespace AvaloniaLoudnessMeterX;

public class LargeLabelControl : TemplatedControl
{
    public static readonly StyledProperty<string> LargeTextProperty =
        AvaloniaProperty.Register<LargeLabelControl, string>(
            nameof(LargeText), nameof(LargeText).ToUpper());

    public static readonly StyledProperty<string> SmallTextProperty =
        AvaloniaProperty.Register<LargeLabelControl, string>(
            nameof(SmallText), nameof(SmallText).ToUpper());

    public string LargeText
    {
        get => GetValue(LargeTextProperty);
        set => SetValue(LargeTextProperty, value);
    }

    public string SmallText
    {
        get => GetValue(SmallTextProperty);
        set => SetValue(SmallTextProperty, value);
    }
}