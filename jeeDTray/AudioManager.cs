using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AudioSwitcher.AudioApi.CoreAudio;


namespace jeeDTray {
    internal class AudioManager : IDisposable {
        private CoreAudioController _controller;
        private ConcurrentDictionary<string, CoreAudioDevice> _deviceCache;
        private object _lockObject = new object();

        public AudioManager() {
            _controller = new CoreAudioController();
            _deviceCache = new ConcurrentDictionary<string, CoreAudioDevice>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetProcessVolume(string[] processus, int volume) {
            if(processus == null || processus.Length == 0 || !IsValidVolume(volume))
                return;

            foreach(string processName in processus) {
                if(!string.IsNullOrEmpty(processName)) {
                    SetApplicationVolume(processName, volume);
                }
            }
        }

        public void SetMasterVolume(int volume) {
            if(!IsValidVolume(volume))
                return;

            var device = _controller.GetDefaultDevice(AudioSwitcher.AudioApi.DeviceType.Playback, AudioSwitcher.AudioApi.Role.Multimedia);
            if(device == null || Math.Abs(device.Volume - volume) < 0.01)
                return;

            device.Volume = volume;
            Logger.Info($"Master volume changed to \"{volume}%\"");
        }

        public CoreAudioDevice GetDevice(string name) {
            if(string.IsNullOrEmpty(name))
                return null;

            return _deviceCache.GetOrAdd(name, key => {
                return _controller.GetPlaybackDevices()
                    .FirstOrDefault(a =>
                        string.Equals(a.Name, key, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a.FullName, key, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a.InterfaceName, key, StringComparison.OrdinalIgnoreCase));
            });
        }

        public void SetDefaultAudioDevice(Guid deviceGuid) {
            if(deviceGuid == Guid.Empty)
                return;

            lock(_lockObject) {
                var device = _controller.GetDevice(deviceGuid);
                if(device == null)
                    return;

                _controller.SetDefaultDevice(device);
                _controller.SetDefaultCommunicationsDevice(device);
            }
        }

        public void SetDefaultAudioDevice(string deviceName) {
            if(string.IsNullOrEmpty(deviceName))
                return;

            var device = GetDevice(deviceName);
            if(device != null) {
                SetDefaultAudioDevice(device.Id);
            }
        }

        public void SetApplicationVolume(string processName, int volume) {
            if(string.IsNullOrEmpty(processName) || !IsValidVolume(volume))
                return;

            var device = _controller.GetDefaultDevice(AudioSwitcher.AudioApi.DeviceType.Playback,
                                                    AudioSwitcher.AudioApi.Role.Multimedia);
            if(device == null)
                return;

            var sessions = device.SessionController.All();
            var processFound = false;

            foreach(var session in sessions) {
                if(session.ProcessId == 0)
                    continue;

                try {
                    using(var process = Process.GetProcessById(session.ProcessId)) {
                        if(process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)) {
                            if(Math.Abs(session.Volume - volume) < 0.01)
                                return;

                            session.Volume = volume;
                            processFound = true;
                            break;
                        }
                    }
                } catch(ArgumentException) { } // Process may have terminated
                catch(InvalidOperationException) { } // Process may have terminated
            }

            if(processFound) {
                Logger.Info($"Changed volume to \"{volume}%\" for process: {processName}");
            }
        }

        private static bool IsValidVolume(int volume) {
            return volume >= 0 && volume <= 100;
        }

        public void Dispose() {
            _deviceCache.Clear();
            _controller?.Dispose();
        }

        ~AudioManager() {
            Dispose();
        }
    }
}