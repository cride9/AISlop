using System.Text.Json;
using System.Xml;

namespace AISlop;

public class Settings
{
    public string model_name { get; set; }
    public bool generate_log { get; set; }
    public bool display_thought { get; set; }
    public bool display_toolcall { get; set; }
    public string ollama_url { get; set; }

}
public static class Config
{
    private static string configPath;
    private static Settings _settings;

    static Config()
    {
        string solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        string configDir = Path.Combine(solutionDir, "config");

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        configPath = Path.Combine(configDir, "config.json");
    }

    public static Settings Settings
    {
        get
        {
            if (_settings == null) LoadConfig();
            return _settings;
        }
    }

    public static void LoadConfig()
    {
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            _settings = JsonSerializer.Deserialize<Settings>(json);
        }
        else
        {
            _settings = new Settings
            {
                model_name = "default_model",
                generate_log = false,
                display_thought = false,
                display_toolcall = false,
                ollama_url = "http://localhost:11434"
            };
            Console.WriteLine("Please change the default config file in the configs folder");
            SaveConfig();
        }
    }

    public static void SaveConfig()
    {
        string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }
}
