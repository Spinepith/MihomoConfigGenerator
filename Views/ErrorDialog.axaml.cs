using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NetCoreAudio;

namespace XKeenMihomoGenerator;

internal partial class ErrorDialog : Window {
    private Player? player;

    public ErrorDialog() {
        InitializeComponent();

        var screen = Screens.Primary;
        MaxWidth = screen!.Bounds.Width * 0.4;
        MaxHeight = screen!.Bounds.Height * 0.6;

        Opened += (_, _) => {
            errorImage.MaxWidth = closeErrorButton.Bounds.Width;
            errorImage.MaxHeight = closeErrorButton.Bounds.Height;

            player = new Player();
            player.Play(Path.Combine("Assets", "Sounds", "error.wav"));
        };
    }

    public static async Task Show(Window owner, string message) {
        var dialog = new ErrorDialog();
        dialog.Message.Text = message;
        await dialog.ShowDialog(owner);
    }

    private void TitleBarPointerPressed(object? sender, PointerPressedEventArgs e) {
        BeginMoveDrag(e);
    }

    private void CloseDialog(object? sender, RoutedEventArgs e) {
        Close();
    }
}