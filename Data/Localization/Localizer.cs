using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace XKeenMihomoGenerator.Data.Localization;

public class Localizer : INotifyPropertyChanged {
    public static Localizer Instance { get; } = new Localizer();
    public event PropertyChangedEventHandler? PropertyChanged;
    private Dictionary<string, object> translations = new Dictionary<string, object>();

    public string this[string key] {
        get {
            string[] parts = key.Split('.');
            object current = translations;

            foreach(string part in parts) {
                if (current is Dictionary<string, object> dict && dict.ContainsKey(part))
                    current = dict[part];
                else
                    return $"[{key}]";
            }

            return current.ToString() ?? $"[{key}]";
        }
    }

    public void SetLanguage(string languageCode) {
        string fileName = Path.Combine("Data", "Localization", $"{languageCode}.json");

        if (File.Exists(fileName)) {
            string text = File.ReadAllText(fileName);

            var temp = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(text);
            translations = new Dictionary<string, object>();

            if (temp != null)
                foreach (var kvp in temp)
                    translations[kvp.Key] = ConvertSimple(kvp.Value);
        }
        else
            translations = new Dictionary<string, object>();

        OnPropertyChanged("Item");
    }
    
    private void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private object ConvertSimple(JsonElement element) {
        if (element.ValueKind == JsonValueKind.Object) {
            var dict = new Dictionary<string, object>();

            foreach (var prop in element.EnumerateObject())
                dict[prop.Name] = ConvertSimple(prop.Value);

            return dict;
        }
        return element.ToString();
    }
}
