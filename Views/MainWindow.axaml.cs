using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MihomoProxyGenerator.Data.Localization;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace MihomoProxyGenerator.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        ConfigureInterface();
    }

    /* LEFT PANEL */
    private void OpenInfoPanel(object? sender, RoutedEventArgs e) {
        infoPanel.IsVisible = true;
    }

    private void CloseInfoPanel(object? sender, RoutedEventArgs e) {
        infoPanel.IsVisible = false;
    }

    private void ChangeLanguage(object? sender, RoutedEventArgs e) {
        Button? button = sender as Button;
        string languageCode = "ENG";

        if (button != null) {
            switch (button.Content) {
                case "RUS":
                    languageCode = "Russian";
                    break;
                case "ENG":
                    languageCode = "English";
                    break;
            }
        }

        Localizer.Instance.SetLanguage(languageCode);
    }

    private void NewConfig(object? sender, RoutedEventArgs e) {
        if (newToggleButton.IsChecked != null) {
            appendToggleButton.IsEnabled = (bool)!newToggleButton.IsChecked;
            appendToggleButton.IsChecked = false;
        }
    }

    /* CENTER PANEL */
    private void LeftPanel(object? sender, RoutedEventArgs e) {
        leftPanelGrid.IsVisible = !leftPanelGrid.IsVisible;
        leftPanelSplitter.IsVisible = !leftPanelSplitter.IsVisible;

        Thickness currentMargin = mainGrid.Margin;

        if (leftPanelButton.IsChecked == true) {
            mainGrid.ColumnDefinitions[0].Width = new GridLength(3, GridUnitType.Star);
            mainGrid.ColumnDefinitions[1].Width = GridLength.Auto;
            mainGrid.Margin = new Thickness(10, currentMargin.Top, currentMargin.Right, currentMargin.Bottom);
        }
        else {
            mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
            mainGrid.ColumnDefinitions[1].Width = new GridLength(0);
            mainGrid.Margin = new Thickness(-30, currentMargin.Top, currentMargin.Right, currentMargin.Bottom);
        }
    }

    private void RightPanel(object? sender, RoutedEventArgs e) {
        rightPanelGrid.IsVisible = !rightPanelGrid.IsVisible;
        rightPanelSplitter.IsVisible = !rightPanelSplitter.IsVisible;

        Thickness currentMargin = mainGrid.Margin;

        if (rightPanelButton.IsChecked == true) {
            mainGrid.ColumnDefinitions[3].Width = GridLength.Auto;
            mainGrid.ColumnDefinitions[4].Width = new GridLength(2, GridUnitType.Star);
            mainGrid.Margin = new Thickness(currentMargin.Left, currentMargin.Top, 10, currentMargin.Bottom);
        }
        else {
            mainGrid.ColumnDefinitions[3].Width = new GridLength(0);
            mainGrid.ColumnDefinitions[4].Width = new GridLength(0);
            mainGrid.Margin = new Thickness(currentMargin.Left, currentMargin.Top, -30, currentMargin.Bottom);
        }
    }

    private async void Browse(object? sender, RoutedEventArgs e) {
        TopLevel? topLevel = GetTopLevel(this);

        if (topLevel?.StorageProvider is null) {
            await MessageBoxManager.GetMessageBoxStandard(
                "Error",
                "StorageProvider is not available",
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error
            ).ShowAsync();
            return;
        }

        //var fileTypes = new FilePickerFileType("Text files")
    }

    private void AppendToConfig(object? sender, RoutedEventArgs e) {
        if (appendToggleButton.IsChecked != null) {
            newToggleButton.IsEnabled = (bool)!appendToggleButton.IsChecked;
            newToggleButton.IsChecked = false;
        }
    }

    /* RIGHT PANEL */
    private async void ShowNewServers(object? sender, RoutedEventArgs e) {
        routerButtonsPanel.IsVisible = false;
    }

    private async void ShowMyServers(object? sender, RoutedEventArgs e) {
        routerButtonsPanel.IsVisible = true;
    }

    /* FUNCTIONS THAT PREPARE THE INTERFACE */
    private void ConfigureInterface() {
        // LOCALIZATION
        Localizer.Instance.SetLanguage(GetSystemLanguage());

        // HIDING THE LEFT MENU
        Thickness currentMargin = mainGrid.Margin;
        leftPanelGrid.IsVisible = !leftPanelGrid.IsVisible;
        leftPanelSplitter.IsVisible = !leftPanelSplitter.IsVisible;
        mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
        mainGrid.ColumnDefinitions[1].Width = new GridLength(0);
        mainGrid.Margin = new Thickness(-30, currentMargin.Top, currentMargin.Right, currentMargin.Bottom);

        // OPEN PROXY SETTINGS DROPDOWN PANEL
        proxySettingsDropdown.IsOpen = true;

        // BLOCK SAVE BUTTON
        saveButton.IsEnabled = false;

        // RIGHT MENU
        rightPanelButton.IsChecked = true;
        myServersButton.IsChecked = true;
    }

    private string GetSystemLanguage() {
        string language = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        
        switch (language) {
            case "ru":
                ruToggleButton.IsChecked = true;
                return "Russian";

            case "en":
                enToggleButton.IsChecked = true;
                return "English";

            default:
                return "English";
        }
    }
}