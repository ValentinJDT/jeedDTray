using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace jeeDTray
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();

            ni.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/icon.ico")).Stream);
            ni.Visible = true;
            ni.Text = "jeeD";

            ni.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            ni.ContextMenuStrip.Items.Add("Quitter", null, (s, a) => { 
                Logger.Info("Exiting service...");
                ni.Visible = false; 
                Current.Shutdown(); 
            });

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            (MainWindow as MainWindow).Stop();
            base.OnExit(e);
        }
    }
}
