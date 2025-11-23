using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace MihomoProxyGenerator.Controls;

public class TextLabel : TemplatedControl {
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<TextLabel, string>(nameof(Label), "Label");
    public string Label {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<TextLabel, string>(nameof(Text), "");
    public string Text {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<string> EndLabelProperty = AvaloniaProperty.Register<TextLabel, string>(nameof(Label), "");
    public string EndLabel {
        get => GetValue(EndLabelProperty);
        set => SetValue(EndLabelProperty, value);
    }

    public static readonly StyledProperty<HorizontalAlignment> ContentHorizontalAlignmentProperty =
            AvaloniaProperty.Register<TextLabel, HorizontalAlignment>(nameof(ContentHorizontalAlignment), HorizontalAlignment.Left);
    public HorizontalAlignment ContentHorizontalAlignment {
        get => GetValue(ContentHorizontalAlignmentProperty);
        set => SetValue(ContentHorizontalAlignmentProperty, value);
    }

    static TextLabel() {
        AffectsRender<TextLabel>(TextProperty);
    }
}