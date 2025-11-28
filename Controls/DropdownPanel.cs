using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;

namespace XKeenMihomoGenerator.Controls;

public class DropdownPanel : TemplatedControl {
    private Grid? mainGrid;
    private TextBlock? leftArrow;
    private TextBlock? rightArrow;
    private ToggleButton? button;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);

        mainGrid = e.NameScope.Find<Grid>("PART_MainGrid");

        leftArrow = e.NameScope.Find<TextBlock>("leftArrow");
        rightArrow = e.NameScope.Find<TextBlock>("rightArrow");

        button = e.NameScope.Find<ToggleButton>("PART_Button");
        if (button != null) {
            button.Click += DropdownPanelButton;
            DropdownPanelButton(null, e);
        }
    }

    public static readonly StyledProperty<string> ButtonTextProperty = AvaloniaProperty.Register<DropdownPanel, string>(nameof(ButtonText), "Выберите");
    public string ButtonText {
        get => GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }

    public static readonly StyledProperty<Thickness> ButtonPaddingProperty = AvaloniaProperty.Register<DropdownPanel, Thickness>(nameof(ButtonPadding), new Thickness(4));
    public Thickness ButtonPadding {
        get => GetValue(ButtonPaddingProperty);
        set => SetValue(ButtonPaddingProperty, value);
    }

    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<DropdownPanel, bool>(nameof(IsOpen), false);
    public bool IsOpen {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly StyledProperty<Control> DropdownContentProperty = AvaloniaProperty.Register<DropdownPanel, Control>(nameof(DropdownContent));
    [Content]
    public Control DropdownContent {
        get => GetValue(DropdownContentProperty);
        set => SetValue(DropdownContentProperty, value);
    }


    private void DropdownPanelButton(object? sender, RoutedEventArgs e) {
        if (leftArrow != null && rightArrow != null) {
            if (IsOpen) {
                leftArrow.Text = "↑";
                rightArrow.Text = "↑";
            }
            else {
                leftArrow.Text = "↓";
                rightArrow.Text = "↓";
            }
        }

        if (mainGrid != null)
            mainGrid.Background = IsOpen ? new SolidColorBrush(Color.Parse("#5459C5")) : new SolidColorBrush(Color.Parse("#8C92FF"));
    }
}