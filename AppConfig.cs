using System.Text.Json;

namespace RimWorldBackup;

public class AppConfig
{
    public string? SteamPath { get; set; }
    public string Language { get; set; } = "zh";
    public BackupOptions Options { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static AppConfig Load(string rootDir)
    {
        var path = Path.Combine(rootDir, "config.json");
        var oldPath = Path.Combine(rootDir, "cfg.xml");

        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppConfig>(json, JsonOpts) ?? new AppConfig();
            }
            catch { }
        }

        // migrate from old XML config
        if (File.Exists(oldPath))
        {
            try
            {
                var cfg = MigrateFromXml(oldPath);
                cfg.Save(rootDir);
                return cfg;
            }
            catch { }
        }

        return new AppConfig();
    }

    public void Save(string rootDir)
    {
        var path = Path.Combine(rootDir, "config.json");
        var json = JsonSerializer.Serialize(this, JsonOpts);
        File.WriteAllText(path, json);
    }

    private static AppConfig MigrateFromXml(string xmlPath)
    {
        var cfg = new AppConfig();
        var doc = new System.Xml.XmlDocument();
        doc.Load(xmlPath);
        var root = doc.DocumentElement;

        cfg.SteamPath = root?.SelectSingleNode("S")?.InnerText;
        cfg.Language = root?.SelectSingleNode("L")?.InnerText ?? "zh";

        var optsNode = root?.SelectSingleNode("O");
        if (optsNode != null)
        {
            cfg.Options.Config = optsNode.SelectSingleNode("C") != null;
            cfg.Options.Saves = optsNode.SelectSingleNode("S") != null;
            cfg.Options.WorkshopMods = optsNode.SelectSingleNode("W") != null;
            cfg.Options.LocalMods = optsNode.SelectSingleNode("L") != null;
            cfg.Options.GameInfo = optsNode.SelectSingleNode("G") != null;
            cfg.Options.PlayerLog = optsNode.SelectSingleNode("P") != null;
            cfg.Options.GameFiles = optsNode.SelectSingleNode("Z") != null;
        }

        return cfg;
    }
}
