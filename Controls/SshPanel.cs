using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using System;

namespace XKeenMihomoGenerator.Controls;

internal class SshPanel : TemplatedControl {
    public static readonly StyledProperty<string> ConnectionStatusProperty = AvaloniaProperty.Register<SshPanel, string>(nameof(ConnectionStatus));
    public string ConnectionStatus {
        get => GetValue(ConnectionStatusProperty);
        set => SetValue(ConnectionStatusProperty, value);
    }

    public static readonly StyledProperty<bool> ShowConnectionStatusProperty = AvaloniaProperty.Register<SshPanel, bool>(nameof(ShowConnectionStatus), false);
    public bool ShowConnectionStatus {
        get => GetValue(ShowConnectionStatusProperty);
        set => SetValue(ShowConnectionStatusProperty, value);
    }

    public static readonly StyledProperty<string> UsernameProperty = AvaloniaProperty.Register<SshPanel, string>(nameof(Username));
    public string Username {
        get => GetValue(UsernameProperty);
        set => SetValue(UsernameProperty, value);
    }

    public static readonly StyledProperty<string> IpProperty = AvaloniaProperty.Register<SshPanel, string>(nameof(Ip));
    public string Ip {
        get => GetValue(IpProperty);
        set => SetValue(IpProperty, value);
    }

    public static readonly StyledProperty<string> PasswordProperty = AvaloniaProperty.Register<SshPanel, string>(nameof(Password));
    public string Password {
        get => GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public static readonly StyledProperty<string> PortProperty = AvaloniaProperty.Register<SshPanel, string>(nameof(Port));
    public string Port {
        get => GetValue(PortProperty);
        set => SetValue(PortProperty, value);
    }

    public static readonly StyledProperty<bool> ShowConnectionButtonProperty = AvaloniaProperty.Register<SshPanel, bool>(nameof(ShowConnectionButton), true);
    public bool ShowConnectionButton {
        get => GetValue(ShowConnectionButtonProperty);
        set => SetValue(ShowConnectionButtonProperty, value);
    }

    public static readonly StyledProperty<bool> EnableConnectionButtonProperty = AvaloniaProperty.Register<SshPanel, bool>(nameof(EnableConnectionButton), true);
    public bool EnableConnectionButton {
        get => GetValue(EnableConnectionButtonProperty);
        set => SetValue(EnableConnectionButtonProperty, value);
    }    

    public static readonly RoutedEvent<RoutedEventArgs> CloseEvent =
        RoutedEvent.Register<SshPanel, RoutedEventArgs>(nameof(ConnectionRequested), RoutingStrategies.Bubble);
    public event EventHandler<RoutedEventArgs> CloseRequested {
        add => AddHandler(CloseEvent, value);
        remove => RemoveHandler(CloseEvent, value);
    }

    public static readonly RoutedEvent<RoutedEventArgs> ConnectionEvent =
        RoutedEvent.Register<SshPanel, RoutedEventArgs>(nameof(ConnectionRequested), RoutingStrategies.Bubble);
    public event EventHandler<RoutedEventArgs> ConnectionRequested {
        add => AddHandler(ConnectionEvent, value);
        remove => RemoveHandler(ConnectionEvent, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);

        Button? closeButton = e.NameScope.Find<Button>("closeSshButton");
        if (closeButton != null)
            closeButton.Click += (_, _) => RaiseEvent(new RoutedEventArgs(CloseEvent));

        Button? connectionButton = e.NameScope.Find<Button>("connectionButton");
        if (connectionButton != null)
            connectionButton.Click += (_, _) => RaiseEvent(new RoutedEventArgs(ConnectionEvent));
    }
}