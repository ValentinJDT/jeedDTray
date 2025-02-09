using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace jeeDTray
{
    internal class Configuration
    {
        public const string CONFIG_DIR = "C:/jeeD/";
        const string CONFIG_FILE_NAME = "jeed.config.json";
        const string CONFIG_FILE = CONFIG_DIR + CONFIG_FILE_NAME;

        public Dictionary<string, List<string>> apps = new Dictionary<string, List<string>>();

        public Dictionary<string, string> devices = new Dictionary<string, string>();

        [JsonProperty("com_port")]
        public string comPort = "COM1";

        [JsonProperty("baud_rate")]
        public int baudRate = 9600;

        [JsonProperty("read_interval")]
        public int readInterval = 400;

        [JsonProperty("debug")]
        public bool debug = false;

        public void save()
        {
            createDir();
            string output = JsonConvert.SerializeObject(this);
            File.WriteAllText(CONFIG_FILE, output);
        }

        public static Configuration load()
        {
            if(File.Exists(CONFIG_FILE))
            {
                Logger.Info("Loading configuration from \"" + CONFIG_FILE_NAME + "\"...");
                string content = File.ReadAllText(CONFIG_FILE);
                Configuration config = JsonConvert.DeserializeObject<Configuration>(content);
                Logger.Info("Configuration loaded !");
                return config;
            } else
            {
                Configuration config = new Configuration();
                Logger.Info("Creating configuration file \"" + CONFIG_FILE_NAME + "\"...");
                config.save();
                Logger.Info("Configuration file created...");
                return config;
            }

        }

        private static void createDir()
        {
            if(!Directory.Exists(CONFIG_DIR))
            {
                Directory.CreateDirectory(CONFIG_DIR);
            }
        }
    }
}
