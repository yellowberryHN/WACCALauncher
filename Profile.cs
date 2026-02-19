using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WACCALauncher
{
    public enum ProfileType
    {
        Generic,
        WACCA
    }

    public class ProfileLoadException : Exception
    {
        public ProfileLoadException(string message) : base(message) { }
    }

    [JsonObject]
    public class Profile
    {
        [JsonIgnore]
        public string ConfigPath;

        [JsonProperty, JsonRequired]
        public string Name { get; set; }

        [JsonProperty, JsonRequired]
        public ProfileType Type { get; set; }

        [JsonProperty, JsonRequired]
        public string BasePath;

        [JsonProperty, JsonRequired]
        public string GamePath;

        [JsonProperty]
        public string UpdaterPath;

        [JsonProperty]
        public string UpdaterArgs;

        [JsonProperty]
        public string ConsoleColor;
        
        [JsonProperty]
        public string PanelColor;

        public DirectoryInfo GetBaseDir()
        {
            return new DirectoryInfo(BasePath);
        }

        public string GetGamePath()
        {
            return Path.Combine(BasePath, GamePath);
        }

        // for WACCA type

        [JsonProperty]
        public List<string> Configs { get; set; } = new List<string>();

        [JsonProperty]
        public bool InjectGame = true;

        [JsonProperty, Obsolete("Use InjectDLLs instead.", true)]
        public string InjectDLL
        {
            get => null;
            set => InjectDLLs.Add(value);
        }

        [JsonProperty]
        public List<string> InjectDLLs = new List<string> { "mercuryhook.dll" };

        public string GetInjectDLLArgs()
        {
            return string.Join(" ", InjectDLLs.Select(s => $"-k {s}"));
        }

        public string GetAmdaemonArgs()
        {
            return string.Format("-f -c {0}", string.Join(" ", Configs));
        }

        // static methods

        public static Profile LoadFromJson(string path)
        {
            var loaded = JsonConvert.DeserializeObject<Profile>(File.ReadAllText(path));
            loaded.ConfigPath = path;

            if (!Directory.Exists(loaded.BasePath))
            {
                throw new ProfileLoadException("Base dir not found");
            }
            if (!File.Exists(loaded.GetGamePath()))
            {
                throw new ProfileLoadException("Game file not found");
            }

            if (loaded.Type == ProfileType.WACCA)
            {
                var binPath = Path.Combine(loaded.BasePath, "bin");
                if (!CheckAmdaemonConfigs(binPath, loaded.Configs))
                {
                    throw new ProfileLoadException("configs missing, check config and files");
                }
                if (!CheckForInjector(binPath, loaded.InjectDLLs))
                {
                    throw new ProfileLoadException("Segatools missing, check config and files");
                }
            }

            return loaded;
        }

        public void SaveToJson()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        private static bool CheckForInjector(string path, List<string> dlls)
        {
            return dlls.TrueForAll(dll => File.Exists(Path.Combine(path, dll))) &&
                   File.Exists(Path.Combine(path, "segatools.ini")) &&
                   File.Exists(Path.Combine(path, "inject.exe"));
        }

        public static bool CheckAmdaemonConfigs(string path, List<string> configs)
        {
            int missingFiles = 0;

            foreach (var cfg in configs)
            {
                if (!File.Exists(Path.Combine(path, cfg))) missingFiles++;
            }

            return missingFiles == 0;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
