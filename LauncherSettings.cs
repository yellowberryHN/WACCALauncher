using Newtonsoft.Json;
using System.IO;

namespace WACCALauncher
{
    // TODO: implement themes
    public enum LauncherTheme
    {
        Dark
    }

    [JsonObject]
    public class LauncherSettings
    {
        [JsonProperty] public string DefaultProfile { get; set; } = string.Empty;
        [JsonProperty] public bool UseWatchdog { get; set; } = true;
        [JsonProperty] public LauncherTheme Theme { get; set; } = LauncherTheme.Dark;
        [JsonProperty] public bool StrictMode { get; set; } = true;
        [JsonProperty] public string ProfileDir { get; set; } = "_profiles";
        [JsonProperty] public bool DisableIO4 { get; set; } = false;

        public static LauncherSettings Load()
        {
            return JsonConvert.DeserializeObject<LauncherSettings>(File.ReadAllText("launcher.json"));
        }

        public static void Save(LauncherSettings settings)
        {
            if (settings == null) settings = new LauncherSettings();

            File.WriteAllText("launcher.json", JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }
}