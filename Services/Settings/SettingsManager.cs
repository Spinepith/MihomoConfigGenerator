namespace XKeenMihomoGenerator.Services.Settings;

internal class SettingsManager {
    public Settings Settings { get; }

    public SettingsManager() {
        Settings = new Settings().Load();
    }
}
