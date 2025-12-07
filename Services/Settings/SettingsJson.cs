using System.IO;
using System.Reflection;
using System.Text.Json;

namespace XKeenMihomoGenerator.Services.Settings;

internal abstract class SettingsJson<T> where T : new() {
    private string filePath;

    public SettingsJson(string filePath) {
        this.filePath = filePath;
    }

    public void Load(T target) {
        if (!File.Exists(filePath)) {
            Save(target);
            return;
        }

        try {
            string json = File.ReadAllText(filePath);
            var loaded = JsonSerializer.Deserialize<T>(json);

            if (loaded != null) {
                foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                    if (property.CanRead && property.CanWrite) {
                        var value = property.GetValue(loaded);
                        property.SetValue(target, value);
                    }
                }
            }
        }
        catch {
            Save(target);
        }
    }

    public void Save(T obj) {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, json);
    }

    public abstract T Load();
    public abstract void Save();
}
