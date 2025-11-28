using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;

namespace XKeenMihomoGenerator.Controls;

public class TextLabel : TemplatedControl {
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);

        if (LabelHalfWidth) {
            Grid? grid = e.NameScope.Get<Grid>("PART_Grid");

            Dispatcher.UIThread.Post(() => {
                grid.ColumnDefinitions[1].MinWidth = grid.ColumnDefinitions[0].ActualWidth;
            }, DispatcherPriority.Loaded);
        }
    }

    public static readonly StyledProperty<bool> LabelHalfWidthProperty =
        AvaloniaProperty.Register<TextLabel, bool>(nameof(LabelHalfWidth), true);
    public bool LabelHalfWidth {
        get => GetValue(LabelHalfWidthProperty);
        set => SetValue(LabelHalfWidthProperty, value);
    }

    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<TextLabel, string>(nameof(Label), "Label");
    public string Label {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<TextBox, string>(nameof(Text), "");
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