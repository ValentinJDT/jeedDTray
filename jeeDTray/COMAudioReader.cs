using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Timers;

namespace jeeDTray {
    internal class COMAudioReader : IDisposable {
        private readonly SerialPort _port;
        private readonly Timer _timer;
        private readonly bool _debug;
        private readonly ConcurrentQueue<string> _messageQueue;
        private readonly int _bufferSize = 4096;
        private volatile bool _isRunning;

        private string _lastHeadsetValue = string.Empty;
        private string _lastVolumesValue = string.Empty;

        public COMAudioReader(SerialPort port, int readInterval, bool debug = false) {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _debug = debug;
            _messageQueue = new ConcurrentQueue<string>();

            _port.ReadBufferSize = _bufferSize;
            _port.ReceivedBytesThreshold = 1;

            _timer = new Timer(readInterval) {
                AutoReset = true
            };
            _timer.Elapsed += ProcessQueue;

            Logger.Info($"COM Audio Reader initialized to \"{port.PortName}\" at \"{port.BaudRate}\" (baud rate).");
        }

        public event EventHandler<PortEventArgs> PortCloseEventHandler;
        public event EventHandler<AudioEventArgs> DevicesEventHandler;
        public event EventHandler<AudioEventArgs> VolumesEventHandler;

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e) {
            if(!_isRunning) return;

            try {
                while(_port.BytesToRead > 0) {
                    string input = _port.ReadLine();
                    _messageQueue.Enqueue(input);
                }
            } catch(Exception ex) {
                HandleException(ex);
            }
        }

        private void ProcessQueue(object sender, ElapsedEventArgs e) {
            if(!_isRunning) return;

            while(_messageQueue.TryDequeue(out string input)) {
                try {
                    ProcessMessage(input);
                } catch(Exception ex) {
                    HandleException(ex);
                }
            }
        }

        private void ProcessMessage(string input) {
            if(_debug) {
                Logger.Info(input);
            }

            if(input.StartsWith("hds=", StringComparison.Ordinal)) {
                string newValue = input.Substring(4);
                if(newValue != _lastHeadsetValue) {
                    _lastHeadsetValue = newValue;
                    DevicesEventHandler?.Invoke(this, new AudioEventArgs(SplitMessage(newValue)));
                }
            } else if(input.StartsWith("sliders=", StringComparison.Ordinal)) {
                string newValue = input.Substring(8);
                if(newValue != _lastVolumesValue) {
                    _lastVolumesValue = newValue;
                    VolumesEventHandler?.Invoke(this, new AudioEventArgs(SplitMessage(newValue)));
                }
            }
        }

        private static string[] SplitMessage(string value) {
            return value.Contains("|")
                ? value.Split('|')
                : new[] { value };
        }

        private void HandleException(Exception ex) {
            if(_debug) {
                Logger.Error($"{ex.Message}\n{ex.StackTrace}");
            } else {
                Logger.Error(ex.Message);
            }
        }

        public void Start() {
            if(_isRunning) return;

            try {
                if(!_port.IsOpen) {
                    _port.Open();
                }

                _isRunning = true;
                _port.DataReceived += DataReceivedHandler;
                _timer.Start();
            } catch(Exception ex) {
                HandleException(ex);
                Stop();
            }
        }

        public void Stop() {
            if(!_isRunning) return;

            _isRunning = false;
            _timer.Stop();
            _port.DataReceived -= DataReceivedHandler;

            try {
                if(_port.IsOpen) {
                    _port.Close();
                }
            } catch(Exception ex) {
                HandleException(ex);
            }

            PortCloseEventHandler?.Invoke(this, new PortEventArgs());
        }

        public void Dispose() {
            Stop();
            _timer.Dispose();
            _port.Dispose();
        }
    }

    sealed class AudioEventArgs : EventArgs {
        public string[] Values { get; }

        public AudioEventArgs(string[] values) {
            Values = values;
        }
    }

    sealed class PortEventArgs : EventArgs {
    }
}