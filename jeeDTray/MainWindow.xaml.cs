using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace jeeDTray {
    public partial class MainWindow : Window, IDisposable {
        private readonly AudioManager _audioManager;
        private readonly Configuration _config;
        private readonly Dictionary<string, List<string>> _appCache;
        private COMAudioReader _comAudioReader;
        private bool _isDisposed;

        public MainWindow() {
            InitializeComponent();

            try {
                Logger.Info("Starting service...");
                _audioManager = new AudioManager();
                _config = Configuration.load();
                _appCache = new Dictionary<string, List<string>>();

                InitializeSerialPort();
                InitializeAppCache();

                Application.Current.Exit += OnApplicationExit;
                Closing += OnWindowClosing;
            } catch(Exception ex) {
                Logger.Error($"Initialization error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private void InitializeSerialPort() {
            string[] ports = SerialPort.GetPortNames();
            if(!ports.Contains(_config.comPort)) {
                string availablePorts = ports.Length > 0 ? $" Please use: {string.Join(", ", ports)}" : string.Empty;
                Logger.Error($"Port \"{_config.comPort}\" not exists.{availablePorts}");
                Environment.Exit(1);
            }

            var serialPort = new SerialPort(_config.comPort, _config.baudRate, Parity.None, 8, StopBits.One) {
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            _comAudioReader = new COMAudioReader(serialPort, _config.readInterval, _config.debug);
            _comAudioReader.VolumesEventHandler += HandleVolumesEvent;
            _comAudioReader.DevicesEventHandler += HandleDevicesEvent;
            _comAudioReader.PortCloseEventHandler += HandlePortCloseEvent;

            Task.Run(() => _comAudioReader.Start())
                .ContinueWith(t => {
                    if(t.IsFaulted) {
                        Logger.Error($"Failed to start COM reader: {t.Exception?.InnerException?.Message}");
                        Application.Current.Dispatcher.Invoke(() => Environment.Exit(1));
                    }
                });
        }

        private void InitializeAppCache() {
            foreach(var app in _config.apps) {
                _appCache[app.Key] = new List<string>(app.Value);
            }
        }

        private void HandlePortCloseEvent(object sender, PortEventArgs e) {
            Logger.Error($"Port \"{_config.comPort}\" closed.");
            Application.Current.Dispatcher.Invoke(() => Environment.Exit(0));
        }

        private void HandleDevicesEvent(object sender, AudioEventArgs e) {
            if(e?.Values == null) return;

            for(int i = 0; i < e.Values.Length; i++) {
                if(e.Values[i] != "1") continue;

                string indexStr = i.ToString();
                if(!_config.devices.TryGetValue(indexStr, out string deviceName)) continue;

                Logger.Info($"Changing default audio device to \"{deviceName}\".");
                Task.Run(() => _audioManager.SetDefaultAudioDevice(deviceName))
                    .ContinueWith(t => {
                        if(t.IsFaulted) {
                            Logger.Error($"Failed to set audio device: {t.Exception?.InnerException?.Message}");
                        }
                    });
            }
        }

        private void HandleVolumesEvent(object sender, AudioEventArgs e) {
            if(e?.Values == null) return;

            for(int i = 0; i < e.Values.Length; i++) {
                string indexStr = i.ToString();
                if(!_appCache.TryGetValue(indexStr, out List<string> apps)) continue;

                if(!int.TryParse(e.Values[i], out int volume)) continue;

                Task.Run(() => {
                    try {
                        if(apps.Contains("master", StringComparer.OrdinalIgnoreCase)) {
                            _audioManager.SetMasterVolume(volume);
                        } else {
                            _audioManager.SetProcessVolume(apps.ToArray(), volume);
                        }
                    } catch(Exception ex) {
                        Logger.Error($"Volume adjustment error: {ex.Message}");
                    }
                });
            }
        }

        private void OnApplicationExit(object sender, ExitEventArgs e) {
            Dispose();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e) {
            Dispose();
        }

        public void Dispose() {
            if(_isDisposed) return;

            _comAudioReader?.Stop();
            _comAudioReader?.Dispose();
            _audioManager?.Dispose();

            _appCache?.Clear();

            Application.Current.Exit -= OnApplicationExit;
            Closing -= OnWindowClosing;

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }

        ~MainWindow() {
            Dispose();
        }
    }
}
