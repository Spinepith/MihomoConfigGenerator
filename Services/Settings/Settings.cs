using System;
using System.IO;

namespace XKeenMihomoGenerator.Services.Settings; 

internal class Settings : SettingsJson<Settings> {
    // -----> ENTWARE <-----
    public bool Autoconnect { get; set; } = false;
    public bool Autoinsert { get; set; } = true;

    public Settings() : base(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Settings", "settings.json")) { }

    public override Settings Load() {
        Load(this);
        return this;
    }

    public override void Save() {
        Save(this);
    }
}
