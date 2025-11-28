using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using System.Linq;

namespace XKeenMihomoGenerator.Controls;

public class SwitchButton : ItemsControl {
    public static readonly StyledProperty<bool> IsRadioProperty = AvaloniaProperty.Register<SwitchButton, bool>(nameof(IsRadio), true);
    public bool IsRadio {
        get => GetValue(IsRadioProperty);
        set => SetValue(IsRadioProperty, value);
    }

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<SwitchButton, Orientation>(nameof(Orientation), Orientation.Horizontal);
    public Orientation Orientation {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
        base.OnAttachedToVisualTree(e);

        if (IsRadio) {
            var buttons = Items.OfType<ToggleButton>().ToList();
            foreach (var button in buttons)
                button.Click += (sender, _) => {
                    var clicked = (ToggleButton)sender!;

                    foreach (var btn in buttons)
                        if (btn != clicked)
                            btn.IsChecked = false;

                    clicked.IsChecked = true;
                };
        }
    }
}