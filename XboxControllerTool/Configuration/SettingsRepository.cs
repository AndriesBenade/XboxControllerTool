using System.Text.Json;

namespace XboxControllerTool.Configuration;

public sealed class SettingsRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public SettingsRepository(string filePath)
    {
        _filePath = filePath;
    }

    public static SettingsRepository CreateDefault()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XboxControllerTool");
        Directory.CreateDirectory(directory);
        return new SettingsRepository(Path.Combine(directory, "settings.json"));
    }

    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            var defaults = AppSettings.CreateDefault();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);

            if (settings is null || settings.Version != AppSettings.CurrentVersion)
            {
                var defaults = AppSettings.CreateDefault();
                Save(defaults);
                return defaults;
            }

            settings.ClampToValidRanges();
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            QuarantineCorruptFile();
            var defaults = AppSettings.CreateDefault();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(AppSettings settings)
    {
        settings.ClampToValidRanges();
        var json = JsonSerializer.Serialize(settings, SerializerOptions);

        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    private void QuarantineCorruptFile()
    {
        try
        {
            var quarantinePath = _filePath + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Move(_filePath, quarantinePath, overwrite: true);
        }
        catch (IOException)
        {
        }
    }
}
