using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace XKeenMihomoGenerator.Data.Localization;

public class LocalizeExtension : MarkupExtension {
    public string Key { get; set; }
    
    public LocalizeExtension(string key) {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) {
        return new Binding {
            Source = Localizer.Instance,
            Path = $"[{Key}]",
            Mode = BindingMode.OneWay
        };
    }
}
