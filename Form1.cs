using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.ServiceProcess;
using System.Diagnostics;
using System.Linq;

namespace io_lockdown
{
    public partial class Form1 : Form
    {
        private NotifyIcon? trayIcon;
        private LockdownEngine _engine = new LockdownEngine();
        private bool _isLocked = false;
        private System.Windows.Forms.Timer _logTimer;

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        public Form1()
        {
            InitializeComponent();
            _logTimer = new System.Windows.Forms.Timer();
            _logTimer.Interval = 1000;
            _logTimer.Tick += (s, e) => RefreshLogs();
            _logTimer.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            
            // Configura Menu de Contexto explicitamente
            var menu = new ContextMenuStrip();
            menu.Items.Add("Exibir Console", null, (s, ev) => ShowConsole());
            menu.Items.Add("-");
            menu.Items.Add("Sair e Encerrar Tudo", null, (s, ev) => ExitApplication());

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Shield;
            trayIcon.Text = "I/O Lockdown";
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            
            // Clique duplo ou simples para abrir
            trayIcon.MouseClick += (s, ev) => {
                if (ev.Button == MouseButtons.Left) ShowConsole();
            };

            RefreshLogs();
            _engine.Log("Interface de usuário iniciada.");
            
            // Na primeira vez, vamos mostrar a janela para você ver que funcionou
            this.Show();
        }

        private void ShowConsole()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
        }

        private void ExitApplication()
        {
            _engine.Log("Encerrando aplicação e serviço...");
            
            try
            {
                using (var sc = new ServiceController("IOLockdownService"))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        sc.Stop();
                    }
                }
            }
            catch { }

            // Restaura hardware
            _engine.SetNetworkState(true);
            _engine.SetUsbStorageState(true);
            _engine.SetUsbHardwareState(true);

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            Environment.Exit(0);
        }

        private void RefreshLogs()
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lockdown.log");
                if (File.Exists(logPath))
                {
                    using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        string content = sr.ReadToEnd();
                        if (rtbLogs.Text != content)
                        {
                            rtbLogs.Text = content;
                            rtbLogs.SelectionStart = rtbLogs.Text.Length;
                            rtbLogs.ScrollToCaret();
                        }
                    }
                }
            }
            catch { }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (_isLocked && m.Msg == WM_DEVICECHANGE)
            {
                int eventType = m.WParam.ToInt32();
                if (eventType == DBT_DEVICEARRIVAL || eventType == DBT_DEVICEREMOVECOMPLETE)
                {
                    _ = _engine.TriggerViolation("Mudança de hardware.");
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    _isLocked = true;
                    _engine.CaptureHardwareWhitelist();
                    _engine.SetNetworkState(false);
                    _engine.SetUsbStorageState(false);
                    break;
                case SessionSwitchReason.SessionUnlock:
                    _isLocked = false;
                    if (!_engine.ViolationDetected)
                    {
                        _engine.SetUsbStorageState(true);
                        _engine.SetNetworkState(true);
                    }
                    break;
            }
        }
    }
}
