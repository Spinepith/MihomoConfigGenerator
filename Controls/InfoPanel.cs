using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using System;

namespace MihomoProxyGenerator.Controls;

public class InfoPanel : TemplatedControl {
    public static readonly RoutedEvent<RoutedEventArgs> CloseEvent = RoutedEvent.Register<InfoPanel, RoutedEventArgs>("CloseEvent", RoutingStrategies.Bubble);
    public event EventHandler<RoutedEventArgs> CloseRequested {
        add => AddHandler(CloseEvent, value);
        remove => RemoveHandler(CloseEvent, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);

        Button? closeButton = e.NameScope.Find<Button>("closeInfoButton");
        if (closeButton != null)
            closeButton.Click += (_, _) => RaiseEvent(new RoutedEventArgs(CloseEvent));
    }
}