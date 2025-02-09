using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace jeeDTray
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AudioManager audioManager;
        private Configuration config;
        private COMAudioReader comAudioReader;

        public MainWindow()
        {
            Logger.Info("Starting service...");

            audioManager = new AudioManager();
            config = Configuration.load();


            string[] ports = SerialPort.GetPortNames();

            if(!ports.Contains(config.comPort))
            {
                Logger.Error("Port \"" + config.comPort + "\" not exists." + (ports.Length > 0 ? " Please use : " + string.Join(", ", ports) : ""));
                Logger.Info("Stopping service...");
                Environment.Exit(0);
                return;
            }

            comAudioReader = new COMAudioReader(new SerialPort(config.comPort, config.baudRate, Parity.None, 8, StopBits.One), config.readInterval, config.debug);

            comAudioReader.VolumesEventHandler += ComAudioReader_VolumesEventHandler;
            comAudioReader.DevicesEventHandler += ComAudioReader_DevicesEventHandler;
            comAudioReader.PortCloseEventHandler += ComAudioReader_PortCloseEventHandler;

            comAudioReader.Start();
        }


        private void ComAudioReader_PortCloseEventHandler(object sender, PortEventArgs e)
        {
            Logger.Error("Port \"" + config.comPort + "\" closed.");
            Logger.Info("Stopping service...");
            Environment.Exit(0);
        }

        public void Stop()
        {
            comAudioReader.Stop();
        }


        void ComAudioReader_DevicesEventHandler(object sender, AudioEventArgs e)
        {
            foreach(var entries in e.values.Select((value, index) => new { index, value }))
            {
                string deviceName = config.devices[entries.index.ToString()];

                if(entries.value == "1")
                {
                    Logger.Info("Changing default audio device to \"" + deviceName + "\".");
                    audioManager.SetDefaultAudioDevice(deviceName);
                }
            }
        }

        void ComAudioReader_VolumesEventHandler(object sender, AudioEventArgs e)
        {
            foreach(var entries in e.values.Select((value, index) => new { index, value }))
            {
                if(!config.apps.ContainsKey(entries.index.ToString())) continue;

                List<string> apps = config.apps[entries.index.ToString()];

                int volume = Int32.Parse(entries.value);

                if(apps.Any(app => app == "master"))
                {
                    audioManager.SetMasterVolume(volume);
                } else
                {
                    audioManager.SetProcessVolume(apps.ToArray(), volume);
                }
            }
        }
    }
}
