using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetCoreAudio;
using XKeenMihomoGenerator.Data.Localization;
using XKeenMihomoGenerator.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace XKeenMihomoGenerator.Views;

public partial class MainWindow : Window {
    private EntwareClient? entware;
    private Player? player;

    private List<Button>? buttonsToBlock;

    public MainWindow() {
        InitializeComponent();
        ConfigureInterface();
        SizeChanged += MainWindowSizeChanged;
        Closing += MainWindowClosing;
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

            myConfigExpandButton.IsChecked = false;
            ExpandTextBox(myConfigExpandButton, e);
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
            await ShowError(Localizer.Instance["browse.errors.storage"]);
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
                await ShowError($"{Localizer.Instance["browse.errors.read"]}\n{ex.Message}");
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

                    textBoxesGrid.ColumnSpacing = 0;
                }
                else if (button.Name == "rightExpandButton") {
                    resultPanel.IsVisible = true;
                    vlessPanel.IsVisible = false;
                    leftExpandButton.IsChecked = false;

                    textBoxesGrid.ColumnDefinitions[0].Width = new GridLength(0);
                    textBoxesGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

                    textBoxesGrid.ColumnSpacing = 0;
                }
                else if (button.Name == "myConfigExpandButton") {
                    mainGrid.ColumnDefinitions[4].Width = new GridLength(6, GridUnitType.Star);
                }
            }
            else {
                if (button.Name != "myConfigExpandButton") {
                    vlessPanel.IsVisible = true;
                    resultPanel.IsVisible = true;
                    textBoxesGrid.ColumnSpacing = 10;
                    textBoxesGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    textBoxesGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                }
                else {
                    mainGrid.ColumnDefinitions[4].Width = new GridLength(2, GridUnitType.Star);
                }
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

    private async void SaveGeneratedResult(object? sender, RoutedEventArgs e) {
        // РЕАЛИЗОВАТЬ ДОБАВЛЕНИЕ В ФАЙЛ [ СЕЙЧАС ТОЛЬКО ПЕРЕЗАПИСЬ ]
        if (string.IsNullOrWhiteSpace(resultTextBox.Text) || resultTextBox.Text == Localizer.Instance["centerPanel.textBoxes.result"]) {
            saveButton.IsEnabled = false;
            return;
        }

        await Save(resultTextBox.Text);
    }

    /* RIGHT PANEL */
    private void ShowNewServers(object? sender, RoutedEventArgs e) {
        if (newServersBlock.Children.Count > 0)
            newServersPanel.IsVisible = true;
        else
            newServersPanel.IsVisible = false;

        myServersPanel.IsVisible = false;
        routerButtonsPanel.IsVisible = false;
        myConfigExpandButton.IsChecked = false;
        ExpandTextBox(myConfigExpandButton, e);

        rightPanelGrid.RowDefinitions[2].Height = new GridLength(0);
    }

    private void ShowMyServers(object? sender, RoutedEventArgs e) {
        if (entware == null || !entware.CheckConnection())
            myServersPanel.IsVisible = false;
        else
            myServersPanel.IsVisible = true;

        newServersPanel.IsVisible = false;
        routerButtonsPanel.IsVisible = true;

        rightPanelGrid.RowDefinitions[2].Height = GridLength.Auto;
    }

    private void OpenSshPanel(object? sender, RoutedEventArgs e) {
        sshPanel.IsVisible = true;
    }

    private void CloseSshPanel(object? sender, RoutedEventArgs e) {
        sshPanel.IsVisible = false;
        sshData.ShowConnectionStatus = false;
        sshData.ConnectionStatus = "";
    }

    private async void ConnectToRouter(object? sender, RoutedEventArgs e) {
        string username = sshData.Username;
        string ip = sshData.Ip;
        string port = sshData.Port;
        string password = sshData.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(port) || string.IsNullOrWhiteSpace(password)) {
            sshData.ShowConnectionStatus = true;
            sshData.ConnectionStatus = Localizer.Instance["sshPanel.errors.emptyData"];
            return;
        }

        entware = new EntwareClient(username, ip, port, password);
        entware.OnBusyStateChanged += EntwareOnBusyStateChanged;
        entware.OnProgressChanged += EntwareOnProgressChanged;

        sshData.ShowConnectionStatus = true;
        sshData.EnableConnectionButton = false;
        sshData.ConnectionStatus = Localizer.Instance["sshPanel.connecting"];

        string connectionStatus = await entware.ConnectAsync();

        sshData.EnableConnectionButton = true;

        if (connectionStatus == "SUCCESS") {
            if (player is null)
                player = new Player();
            await player.Play(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds", "connected.wav"));

            sshData.ConnectionStatus = Localizer.Instance["sshPanel.success"];
            sshData.ShowConnectionButton = false;

            connectButton.IsVisible = false;
            myServersPanel.IsVisible = true;
            expandConfigUpdateButtons.IsVisible = true;
            reloadMyServersButton.IsVisible = true;
            backupButtons.IsVisible = true;
            disconnectButton.IsVisible = true;

            routerButtonsPanel.RowSpacing = 4;

            buttonsToBlock = new List<Button> {
                showMyConfigButton,
                reloadMyConfigButton,
                reloadMyServersButton,
                saveMyConfigButton,
                backupEntwareButton,
                backupConfigButton,
                disconnectButton,
                restartXKeenButton
            };
        }
        else
            sshData.ConnectionStatus = connectionStatus;
    }

    private async void DisconnectRouter(object? sender, RoutedEventArgs e) {
        entware?.Disconnect();

        if (player is null)
            player = new Player();
        await player.Play(Path.Combine("Assets", "Sounds", "disconnected.wav"));

        myConfigBlock.Text = "";
        myConfigBlock.Tag = null;

        sshData.ShowConnectionButton = true;

        myConfigScrollBar.IsVisible = false;

        saveMyConfigButton.IsEnabled = false;
        showMyConfigButton.IsChecked = false;

        connectButton.IsVisible = true;
        myServersPanel.IsVisible = false;
        expandConfigUpdateButtons.IsVisible = false;
        reloadMyConfigButton.IsVisible = false;
        saveMyConfigButton.IsVisible = false;
        backupButtons.IsVisible = false;
        disconnectButton.IsVisible = false;
        restartXKeenButton.IsVisible = false; ;

        myConfigExpandButton.IsChecked = false;
        ExpandTextBox(myConfigExpandButton, e);

        routerButtonsPanel.RowSpacing = 0;
    }

    private void CancelBackup(object? sender, RoutedEventArgs e) {
        entware?.CancelOperation();
        progressBar.Value = 0;
        progressBarBlock.IsVisible = false;
        cancelBackupButton.IsVisible = false;
        backupButtons.IsVisible = true;
    }

    private async void RestartXKeen(object? sender, RoutedEventArgs e) {
        string? buttonText = (string?)restartXKeenButton.Content;

        if (buttonText != null && entware != null) {
            string on = Localizer.Instance["rightPanel.buttons.XKeenOn"];
            string off = Localizer.Instance["rightPanel.buttons.XKeenOff"];
            string result;

            if (buttonText == on) {
                System.Diagnostics.Debug.WriteLine(1);
                result = await entware.XKeenStartAsync();
                restartXKeenButton.Content = off;
            }
            else {
                System.Diagnostics.Debug.WriteLine(2);
                result = await entware.XKeenStopAsync();
                restartXKeenButton.Content = on;
            }

            if (result.StartsWith(nameof(EntwareClient))) {
                restartXKeenButton.Content = $"{Localizer.Instance["rightPanel.buttons.XKeenOn"]} / {Localizer.Instance["rightPanel.buttons.XKeenOff"]}";
                await ShowError(result);
                return;
            }
        }
    }

    private async void ShowMyConfig(object? sender, RoutedEventArgs e) {
        ToggleButton? button = sender as ToggleButton;
        if (button?.IsChecked != null) {
            myConfigScrollBar.IsVisible = (bool)button.IsChecked;
            reloadMyConfigButton.IsVisible = (bool)button.IsChecked;
            saveMyConfigButton.IsVisible = (bool)button.IsChecked;
            restartXKeenButton.IsVisible = (bool)button.IsChecked;

            reloadMyServersButton.IsVisible = !(bool)button.IsChecked;
            backupButtons.IsVisible = (bool)!button.IsChecked;
            disconnectButton.IsVisible = (bool)!button.IsChecked;

            if (button.IsChecked == true && entware != null) {
                string XKeenStatus = await entware.XKeenStatusAsync();

                if (!XKeenStatus.StartsWith(nameof(EntwareClient))) {
                    if (XKeenStatus == "ON")
                        restartXKeenButton.Content = Localizer.Instance["rightPanel.buttons.XKeenOff"];
                    else if (XKeenStatus == "OFF")
                        restartXKeenButton.Content = Localizer.Instance["rightPanel.buttons.XKeenOn"];
                    else {
                        restartXKeenButton.Content = $"{Localizer.Instance["rightPanel.buttons.XKeenOn"]} / {Localizer.Instance["rightPanel.buttons.XKeenOff"]}";
                        await ShowError(XKeenStatus);
                    }
                }
                else
                    await ShowError(XKeenStatus);

                if (myConfigBlock.Tag as string != "loaded") {
                    string myConfig = await entware.GetUserConfigAsync();

                    if (myConfig.StartsWith(nameof(EntwareClient))) {
                        await ShowError(myConfig);
                        return;
                    }

                    myConfigBlock.Tag = "loaded";
                    myConfigBlock.Text = myConfig;

                    await Task.Delay(10);
                    saveMyConfigButton.IsEnabled = false;

                    buttonsToBlock?.Remove(showMyConfigButton);
                }
            }
        }
    }

    private void OnMyConfigChanged(object? sender, TextChangedEventArgs e) {
        if (myConfigBlock.Tag as string == "loaded")
            saveMyConfigButton.IsEnabled = true;
    }

    private async void SaveMyConfig(object? sender, RoutedEventArgs e) {
        if (entware != null && !string.IsNullOrWhiteSpace(myConfigBlock.Text)) {
            string result = await entware.SaveUserConfigAsync(myConfigBlock.Text);

            if (result.StartsWith(nameof(EntwareClient))) {
                await ShowError(result);
                return;
            }

            saveMyConfigButton.IsEnabled = false;
        }
    }

    private async void Backup(object? sender, RoutedEventArgs e) {
        if (entware != null) {
            backupButtons.IsVisible = false;
            disconnectButton.IsVisible = false;
            progressBarBlock.IsVisible = true;
            cancelBackupButton.IsVisible = true;

            progressBarBlock.Height = backupButtons.Bounds.Height;

            string result;
            if (sender is Button button && button.Name == "backupEntwareButton")
                result = await entware.BackupEntwareAsync();
            else
                result = await entware.BackupConfigAsync();

            if (!result.StartsWith(nameof(EntwareClient)))
                await SaveDownloadedFile(result);
            else
                await ShowError(result);

            progressBar.Value = 0;
            progressBarBlock.IsVisible = false;
            cancelBackupButton.IsVisible = false;
            backupButtons.IsVisible = true;
            disconnectButton.IsVisible = true;
        }
    }

    private async void ReloadMyConfig(object? sender, RoutedEventArgs e) {
        if (entware != null) {
            myConfigBlock.Text = "";
            buttonsToBlock?.Add(showMyConfigButton);

            string myConfig = await entware.GetUserConfigAsync();

            if (myConfig.StartsWith(nameof(EntwareClient))) {
                await ShowError(myConfig);
                return;
            }

            myConfigBlock.Tag = "loaded";
            myConfigBlock.Text = myConfig;

            await Task.Delay(10);
            saveMyConfigButton.IsEnabled = false;

            buttonsToBlock?.Remove(showMyConfigButton);
        }
    }

    private void ReloadMyServers(object? sender, RoutedEventArgs e) {

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
        leftPanelGrid.IsVisible = false;
        leftPanelSplitter.IsVisible = false;
        mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
        mainGrid.ColumnDefinitions[1].Width = new GridLength(0);
        mainGrid.Margin = new Thickness(-30, currentMargin.Top, currentMargin.Right, currentMargin.Bottom);

        // OPEN PROXY SETTINGS DROPDOWN PANEL
        proxySettingsDropdown.IsOpen = true;

        //// CENTER MENU \\\\
        // BLOCK GRID SPLITTERS
        leftPanelSplitter.IsEnabled = false;
        rightPanelSplitter.IsEnabled = false;

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
        expandConfigUpdateButtons.IsVisible = false;
        saveMyConfigButton.IsVisible = false;
        reloadMyConfigButton.IsVisible = false;
        progressBarBlock.IsVisible = false;
        backupButtons.IsVisible = false;
        cancelBackupButton.IsVisible = false;
        disconnectButton.IsVisible = false;
        restartXKeenButton.IsVisible = false;

        saveMyConfigButton.IsEnabled = false;
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

    private async Task Save(string text, bool append = false) {
        // РЕАЛИЗОВАТЬ ДОБАВЛЕНИЕ В ФАЙЛ [ СЕЙЧАС ТОЛЬКО ПЕРЕЗАПИСЬ ]
        TopLevel? topLevel = GetTopLevel(this);

        if (topLevel?.StorageProvider is null) {
            await ShowError(Localizer.Instance["save.errors.storage"]);
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
                await streamWriter.WriteAsync(text);
                saveTextBox.Text = file.Path.LocalPath;
            }
            catch (Exception ex) {
                await ShowError($"{Localizer.Instance["save.errors.save"]}\n{ex.Message}");
            }
        }
    }

    private async Task SaveDownloadedFile(string path) {
        TopLevel? topLevel = GetTopLevel(this);

        if (topLevel?.StorageProvider is null) {
            await ShowError(Localizer.Instance["save.errors.storage"]);
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
                await using (var sourceStream = File.OpenRead(path))
                await using (var destinationStream = await file.OpenWriteAsync()) {
                    await sourceStream.CopyToAsync(destinationStream);
                }
                File.Delete(path);
            }
            catch (Exception ex) {
                await ShowError($"{Localizer.Instance["save.errors.save"]}\n{ex.Message}\n\n{Localizer.Instance["save.errors.toTempDirectory"]}: {path}");
            }
        }
        else
            if (File.Exists(path))
                File.Delete(path);
    }

    private void EntwareOnBusyStateChanged(bool isBusy) {
        if (buttonsToBlock != null)
            Dispatcher.UIThread.Invoke(() => {
                if (isBusy)
                    foreach (Button button in buttonsToBlock) {
                        if (button.Tag == null)
                            button.Tag = button.IsEnabled;
                        button.IsEnabled = false;
                    }
                else
                    foreach (Button button in buttonsToBlock) {
                        if (button.Tag is bool state) {
                            button.IsEnabled = state;
                            button.Tag = null;
                        }
                        else
                            button.IsEnabled = true;
                    }
            });
    }

    private void EntwareOnProgressChanged(double progress, bool isPercent) {
        Dispatcher.UIThread.Invoke(() => {
            if (isPercent) {
                progressBar.Value = progress;
                progressBarText.Text = progress.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
                progressBarText.Text = $"{progress.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} MB";
        });
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

    private async Task ShowError(string message) {
        await ErrorDialog.Show(this, message);
    }

    private void MainWindowSizeChanged(object? sender, SizeChangedEventArgs e) {
        if (e.NewSize.Width < 1200) {
            myConfigExpandButton.IsChecked = false;
            myConfigExpandButton.IsEnabled = false;
            ExpandTextBox(myConfigExpandButton, e);
        }
        else
            myConfigExpandButton.IsEnabled = true;
    }

    private void MainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
        if (entware != null) {
            entware.Dispose();
            System.Diagnostics.Debug.WriteLine("ROUTER DISPOSED");
        }
        base.OnClosed(e);
    }
}