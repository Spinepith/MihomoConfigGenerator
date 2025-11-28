using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using NetCoreAudio;
using XKeenMihomoGenerator.Data.Localization;
using XKeenMihomoGenerator.Services;

namespace XKeenMihomoGenerator.Views;

public partial class MainWindow : Window {
    private EntwareClient? router;
    private Player? player;

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

    private async void Save(object? sender, RoutedEventArgs e) {
        // РЕАЛИЗОВАТЬ ДОБАВЛЕНИЕ В ФАЙЛ [ СЕЙЧАС ТОЛЬКО ПЕРЕЗАПИСЬ ]
        if (string.IsNullOrWhiteSpace(resultTextBox.Text) || resultTextBox.Text == Localizer.Instance["centerPanel.textBoxes.result"]) {
            saveButton.IsEnabled = false;
            return;
        }

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
                await streamWriter.WriteAsync(resultTextBox.Text ?? "");
                 saveTextBox.Text = file.Path.LocalPath;
            }
            catch (Exception ex) {
                await ShowError($"{Localizer.Instance["save.errors.save"]}\n{ex.Message}");
            }
        }
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
        if (router == null || !router.CheckInitialConnection())
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

        router = new EntwareClient(username, ip, port, password);
        
        sshData.ShowConnectionStatus = true;
        sshData.EnableConnectionButton = false;
        sshData.ConnectionStatus = Localizer.Instance["sshPanel.connecting"];

        string connectionStatus = await router.ConnectAsync();

        sshData.EnableConnectionButton = true;

        if (connectionStatus == "SUCCESS") {
            if (player is null)
                player = new Player();
            await player.Play(Path.Combine("Assets", "Sounds", "connected.wav"));
            
            sshData.ConnectionStatus = Localizer.Instance["sshPanel.success"];
            sshData.ShowConnectionButton = false;

            connectButton.IsVisible = false;
            myServersPanel.IsVisible = true;
            showMyConfigButton.IsVisible = true;
            backupEntwareButton.IsVisible = true;
            disconnectButton.IsVisible = true;

            routerButtonsPanel.RowSpacing = 4;
        }
        else
            sshData.ConnectionStatus = connectionStatus;
    }

    private async void DisconnectRouter(object? sender, RoutedEventArgs e) {
        router?.Disconnect();
        
        if (player is null)
            player = new Player();
        await player.Play(Path.Combine("Assets", "Sounds", "disconnected.wav"));

        myConfigBlock.Text = "";
        myConfigBlock.Tag = null;

        sshData.ShowConnectionButton = true;

        reloadMyConfigButton.IsEnabled = false;
        showMyConfigButton.IsChecked = false;

        connectButton.IsVisible = true;
        myServersPanel.IsVisible = false;
        myConfigButtons.IsVisible = false;
        showMyConfigButton.IsVisible = false;
        backupEntwareButton.IsVisible = false;
        disconnectButton.IsVisible = false;

        routerButtonsPanel.RowSpacing = 0;
    }

    private async void ShowMyConfig(object? sender, RoutedEventArgs e) {
        ToggleButton? button = sender as ToggleButton;
        if (button?.IsChecked != null) {
            myConfigScrollBar.IsVisible = (bool)button.IsChecked;
            myConfigButtons.IsVisible = (bool)button.IsChecked;
            backupEntwareButton.IsVisible = (bool)!button.IsChecked;

            myConfigExpandButton.Padding = reloadMyConfigButton.Padding;

            myConfigExpandButton.MaxHeight = connectButton.Bounds.Height;
            reloadMyConfigButton.MaxHeight = connectButton.Bounds.Height;

            if (button.IsChecked == true && router != null && myConfigBlock.Tag as string != "loaded") {
                string myConfig = await router.GetUserConfigAsync();

                if (myConfig.StartsWith(nameof(EntwareClient))) {
                    await ShowError(myConfig);
                    return;
                }

                reloadMyConfigButton.IsEnabled = false;
                myConfigBlock.Text = myConfig;
                myConfigBlock.Tag = "loaded";
            }
            else if (button.IsChecked == false) {
                myConfigExpandButton.IsChecked = false;
                ExpandTextBox(myConfigExpandButton, e);
            }
        }
    }

    private void OnMyConfigChanged(object? sender, TextChangedEventArgs e) {
        if (myConfigBlock.Tag as string == "loaded")
            reloadMyConfigButton.IsEnabled = true;
    }

    private void SaveMyConfig(object? sender, RoutedEventArgs e) {
        reloadMyConfigButton.IsEnabled = false;
    }
    private void ReloadMyConfig(object? sender, RoutedEventArgs e) {
        myConfigBlock.Tag = null;
        reloadMyConfigButton.IsEnabled = false;
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
        myConfigButtons.IsVisible = false;
        showMyConfigButton.IsVisible = false;
        backupEntwareButton.IsVisible = false;
        disconnectButton.IsVisible = false;
        reloadMyConfigButton.IsEnabled = false;
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
        if (router != null) {
            router.Dispose();
            System.Diagnostics.Debug.WriteLine("ROUTER DISPOSED");
        }
    }
}