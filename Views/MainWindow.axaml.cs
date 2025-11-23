using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Avalonia.VisualTree;
using MihomoProxyGenerator.Data.Localization;

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

        var filePickerOptions = new FilePickerOpenOptions {
            Title = Localizer.Instance["browse.title"],
            AllowMultiple = false,
            FileTypeFilter = new[] {
                new FilePickerFileType(Localizer.Instance["browse.fileType"]) {
                    Patterns = new[] { "*.txt" },
                    MimeTypes = new[] { "text/plain" }
                }
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);

        if (files.Count == 1) {
            try {
                var selectedFile = files[0];

                await using var stream = await selectedFile.OpenReadAsync();
                using var streamReader = new StreamReader(stream);
                string content = await streamReader.ReadToEndAsync();

                browseTextBox.Text = selectedFile.Path.LocalPath;
                vlessTextBox.Text = content;
            }
            catch (Exception ex) {
                await MessageBoxManager.GetMessageBoxStandard(
                    "Error",
                    $"Couldn't read file: {ex.Message}",
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error
                ).ShowAsync();
            }
        }
    }

    private void GotFocusTextBox(object? sender, GotFocusEventArgs e) {
        TextBox? textBox = sender as TextBox;

        if (textBox != null) {
            if (textBox.Text == GetPlaceholder(textBox))
                textBox.Text = "";

            textBox.HorizontalContentAlignment = HorizontalAlignment.Left;
            textBox.VerticalContentAlignment = VerticalAlignment.Top;
        }
    }

    private void LostFocusTextBox(object? sender, RoutedEventArgs e) {
        TextBox? textBox = sender as TextBox;
        saveButton.IsEnabled = true;

        if (textBox != null) {
            if (string.IsNullOrWhiteSpace(textBox.Text)) {
                textBox.Text = GetPlaceholder(textBox);
                textBox.HorizontalContentAlignment = HorizontalAlignment.Center;
                textBox.VerticalContentAlignment = VerticalAlignment.Center;
                saveButton.IsEnabled = false;
            }
        }
    }

    private string GetPlaceholder(TextBox textBox) {
        switch (textBox.Name) {
            case "vlessTextBox":
                return Localizer.Instance["centerPanel.textBoxes.vless"];

            case "resultTextBox":
                return Localizer.Instance["centerPanel.textBoxes.result"];

            default:
                return "";
        }
    }

    private void ExpandTextBox(object? sender, RoutedEventArgs e) {
        ToggleButton? button = sender as ToggleButton;

        if (button != null) {
            if (button.IsChecked == true) {
                if (button.Name == "leftExpandButton") {
                    vlessPanel.IsVisible = true;
                    resultPanel.IsVisible = false;
                    rightExpandButton.IsChecked = false;

                    textBoxesGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    textBoxesGrid.ColumnDefinitions[1].Width = new GridLength(0);
                }
                else if (button.Name == "rightExpandButton") {
                    resultPanel.IsVisible = true;
                    vlessPanel.IsVisible = false;
                    leftExpandButton.IsChecked = false;

                    textBoxesGrid.ColumnDefinitions[0].Width = new GridLength(0);
                    textBoxesGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                }

                textBoxesGrid.ColumnSpacing = 0;
            }
            else {
                vlessPanel.IsVisible = true;
                resultPanel.IsVisible = true;
                textBoxesGrid.ColumnSpacing = 10;
                textBoxesGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                textBoxesGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            }
        }
    }

    private void Generate(object? sender, RoutedEventArgs e) {
        if (string.IsNullOrWhiteSpace(resultTextBox.Text) || resultTextBox.Text == Localizer.Instance["centerPanel.textBoxes.result"]) {
            saveButton.IsEnabled = false;
            return;
        }
    }

    private void AppendToConfig(object? sender, RoutedEventArgs e) {
        if (appendToggleButton.IsChecked != null) {
            newToggleButton.IsEnabled = (bool)!appendToggleButton.IsChecked;
            newToggleButton.IsChecked = false;
        }
    }

    private async void Save(object? sender, RoutedEventArgs e) {
        // РЕАЛИЗОВАТЬ ДОБАВЛЕНИЕ В ФАЙЛ [ СЕЙЧАС ТОЛЬКО ПЕРЕЗАПИСЬ ]
        if (string.IsNullOrWhiteSpace(resultTextBox.Text) || resultTextBox.Text == Localizer.Instance["centerPanel.textBoxes.result"]) {
            saveButton.IsEnabled = false;
            return;
        }

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

        var saveOptions = new FilePickerSaveOptions {
            Title = Localizer.Instance["save.title"],
            DefaultExtension = ".yaml",
            FileTypeChoices = new[] {
                new FilePickerFileType("YAML Configuration") {
                    Patterns = new[] { "*.yaml" },
                    MimeTypes = new[] { "application/x-yaml", "text/yaml" }
                }
            }
        };

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(saveOptions);

        if (file is not null) {
            try {
                await using var stream = await file.OpenWriteAsync();
                using var streamWriter = new StreamWriter(stream);
                await streamWriter.WriteAsync(resultTextBox.Text ?? "");
            }
            catch (Exception ex) {
                await MessageBoxManager.GetMessageBoxStandard(
                    "Error",
                    $"Couldn't save file: {ex.Message}",
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error
                ).ShowAsync();
            }
        }
    }

    /* RIGHT PANEL */
    private void ShowNewServers(object? sender, RoutedEventArgs e) {
        routerButtonsPanel.IsVisible = false;
    }

    private void ShowMyServers(object? sender, RoutedEventArgs e) {
        routerButtonsPanel.IsVisible = true;
    }

    void ShowMyConfig(object? sender, RoutedEventArgs e) {
        ToggleButton? button = sender as ToggleButton;
        if (button?.IsChecked != null) {
            myConfigScrollBar.IsVisible = (bool)button.IsChecked;
            myServersScrollBar.IsVisible = (bool)!button.IsChecked;
        }
    }


    /* FUNCTIONS THAT PREPARE THE INTERFACE */
    private void ConfigureInterface() {
        // LOCALIZATION
        Localizer.Instance.SetLanguage(GetSystemLanguage());

        // REMOVE FOCUS FROM TEXTBOX WHEN CLICKING IN ANY AREA
        AddHandler(PointerPressedEvent, OnGlobalPointerPressed, RoutingStrategies.Tunnel, true);

        //// LEFT MENU \\\\
        // HIDING THE LEFT MENU
        Thickness currentMargin = mainGrid.Margin;
        leftPanelGrid.IsVisible = !leftPanelGrid.IsVisible;
        leftPanelSplitter.IsVisible = !leftPanelSplitter.IsVisible;
        mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
        mainGrid.ColumnDefinitions[1].Width = new GridLength(0);
        mainGrid.Margin = new Thickness(-30, currentMargin.Top, currentMargin.Right, currentMargin.Bottom);

        // OPEN PROXY SETTINGS DROPDOWN PANEL
        proxySettingsDropdown.IsOpen = true;

        //// CENTER MENU \\\\
        // CENTERING TEXT BOX && CONTENT INSIDE IT
        vlessTextBox.HorizontalContentAlignment = HorizontalAlignment.Center;
        vlessTextBox.VerticalContentAlignment = VerticalAlignment.Center;
        resultTextBox.HorizontalContentAlignment = HorizontalAlignment.Center;
        resultTextBox.VerticalContentAlignment = VerticalAlignment.Center;

        // BLOCK BROWSE & SAVE TEXTBOXES
        browseTextBox.IsEnabled = false;
        saveTextBox.IsEnabled = false;

        // BLOCK SAVE BUTTON
        saveButton.IsEnabled = false;

        //// RIGHT MENU \\\\
        // SHOW RIGHT MENU
        rightPanelButton.IsChecked = true;
        myServersButton.IsChecked = true;

        // HIDE NEW SERVERS & MY SERVERS PANELS
        newServersPanel.IsVisible = false;
        myServersPanel.IsVisible = false;
        myConfigScrollBar.IsVisible = false;

        // BLOCK ROUTER BUTTONS
        showMyConfigButton.IsVisible = false;
        backupMyConfigButton.IsVisible = false;
        replaceMyConfigButton.IsVisible = false;
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

    private void OnGlobalPointerPressed(object? sender, PointerPressedEventArgs e) {
        var ckickedElement = e.Source as Visual;

        while (ckickedElement != null) {
            if (ckickedElement is TextBox)
                return;
            ckickedElement = ckickedElement.GetVisualParent();
        }

        var topLevel = GetTopLevel(this);
        topLevel?.FocusManager?.ClearFocus();
    }
}