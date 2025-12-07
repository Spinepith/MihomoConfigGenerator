using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace XKeenMihomoGenerator.Controls;

internal class LabeledPanel : TemplatedControl {
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<LabeledPanel, string>(nameof(Label), "Label");
    public string Label {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly StyledProperty<HorizontalAlignment> LabelAlignmentProperty =
            AvaloniaProperty.Register<LabeledPanel, HorizontalAlignment>(nameof(LabelAlignment), HorizontalAlignment.Left);

    public HorizontalAlignment LabelAlignment {
        get => GetValue(LabelAlignmentProperty);
        set => SetValue(LabelAlignmentProperty, value);
    }

    public static readonly StyledProperty<Control> ContentContainerProperty = AvaloniaProperty.Register<LabeledPanel, Control>(nameof(ContentContainer));
    [Content]
    public Control ContentContainer {
        get => GetValue(ContentContainerProperty);
        set => SetValue(ContentContainerProperty, value);
    }
}