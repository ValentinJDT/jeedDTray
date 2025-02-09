using System;
using System.IO.Ports;
using System.Timers;

namespace jeeDTray
{
    internal class COMAudioReader
    {
        private SerialPort port;
        private Timer timer;
        private bool debug;

        /*
        private string oldHeadset = "";
        private string oldVolumes = "";
        */


        public COMAudioReader(SerialPort port, int readInterval, bool debug = false)
        {
            this.port = port;
            this.debug = debug;

            timer = new Timer();
            timer.Elapsed += Reading;
            timer.Interval = readInterval;

            Logger.Info("COM Audio Reader initialized to \"" + port.PortName + "\" at \"" + port.BaudRate + "\" (baud rate).");
        }

        public event EventHandler<PortEventArgs> PortCloseEventHandler;
        public event EventHandler<AudioEventArgs> DevicesEventHandler;
        public event EventHandler<AudioEventArgs> VolumesEventHandler;


        private void Reading(object sender, ElapsedEventArgs e)
        {
            try
            {
                if(!port.IsOpen)
                {
                    this.Stop();
                    PortCloseEventHandler?.Invoke(e, new PortEventArgs());
                    return;
                }

                if(port.BytesToRead <= 0) return;

                string input = port.ReadLine();

                if(this.debug)
                {
                    Logger.Info(input);
                }

                if(input.StartsWith("hds="))
                {
                    /*if(oldHeadset == input) return;
                    oldHeadset = input;*/

                    DevicesEventHandler?.Invoke(e, new AudioEventArgs(splitting(input.Substring(4))));
                } else if(input.StartsWith("sliders="))
                {
                    /*if(oldVolumes == input) return;
                    oldVolumes = input;*/

                    VolumesEventHandler?.Invoke(e, new AudioEventArgs(splitting(input.Substring(8))));
                }

            } catch(Exception ex)
            {
                if(debug)
                {
                    Logger.Error(ex.Message + "\n" + ex.StackTrace);
                } else
                {
                    Logger.Error(ex.Message);
                }
            }

        }

        public void Start()
        {
            if(!port.IsOpen)
            {
                port.Open();
            }

            timer.Start();
        }

        public void Stop()
        {
            if(port.IsOpen)
            {
                port.Close();
            }

            timer.Stop();
        }

        private string[] splitting(string value)
        {
            if(value.Contains("|"))
            {
                return value.Split('|');
            } else
            {
                return new string[] { value };
            }
        }
    }


    class AudioEventArgs : EventArgs
    {
        public string[] values { get; }

        public AudioEventArgs(string[] values)
        {
            this.values = values;
        }
    }

    class PortEventArgs : EventArgs
    {
    }

    class ExceptionEventArgs : EventArgs
    {
        public Exception exception { get; }

        public ExceptionEventArgs(Exception exception)
        {
            this.exception = exception;
        }
    }
}
