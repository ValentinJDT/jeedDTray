using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioSwitcher.AudioApi.CoreAudio;

namespace jeeDTray
{
    internal class AudioManager
    {

        private CoreAudioController controller;

        public AudioManager()
        {
            this.controller = new CoreAudioController();
        }

        public void SetProcessVolume(string[] processus, int volume)
        {
            if(processus.Length == 0) return;
            foreach(string processName in processus)
            {
                SetApplicationVolume(processName, volume);
            }
        }

        public void SetMasterVolume(int volume)
        {
            var defaultPlaybackDevice = controller.DefaultPlaybackDevice;
            if(defaultPlaybackDevice.Volume == volume) return;
            defaultPlaybackDevice.Volume = volume;
            Logger.Info("Master volume changed to \"" + volume + "%\"");

        }

        public CoreAudioDevice GetDevice(string name)
        {
            return controller.GetPlaybackDevices().FirstOrDefault(a => a.Name == name || a.FullName == name || a.InterfaceName == name);
        }

        public void SetDefaultAudioDevice(Guid deviceGuid)
        {
            var device = controller.GetDevice(deviceGuid);
            controller.SetDefaultDevice(device);
            controller.SetDefaultCommunicationsDevice(device);
        }

        public void SetDefaultAudioDevice(string deviceName)
        {
            var device = GetDevice(deviceName);

            if(device != null)
            {
                SetDefaultAudioDevice(device.Id);
            }
        }
        
        public void SetApplicationVolume(string processName, int volume)
        {
            if(volume < 0 || volume > 100)
            {
                return;
            }

            var defaultPlaybackDevice = controller.GetDefaultDevice(AudioSwitcher.AudioApi.DeviceType.Playback, AudioSwitcher.AudioApi.Role.Multimedia);

            var sessions = defaultPlaybackDevice.SessionController.All();


            List<string> processus = new List<string>();
            string text = "Changing volume to \"" + volume + "%\" of : ";
   
            foreach(var session in sessions)
            {
                if(session.ProcessId == 0) continue;

                using(var process = Process.GetProcessById(session.ProcessId))
                {
                    if(process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    {
                        if(session.Volume == volume) return;
                        processus.Add(processName);
                        session.Volume = volume;
                        break;
                    }
                }
            }

            if(processus.Count > 0)
            {
                Logger.Info(text + String.Join(", ", processus.ToArray()));
            }
        }

    }
}
