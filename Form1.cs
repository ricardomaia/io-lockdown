using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;

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
            _logTimer.Interval = 2000;
            _logTimer.Tick += (s, e) => RefreshLogs();
            _logTimer.Start();
        }

        private void DebugLog(string msg)
        {
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "iolockdown_debug.log"), $"[FORM-LOAD] {msg}{Environment.NewLine}"); } catch { }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DebugLog("Iniciando Form1_Load");
            try
            {
                SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
                DebugLog("Eventos de Sessão registrados.");

                var menu = new ContextMenuStrip();
                menu.Items.Add("Exibir Console de Auditoria", null, (s, ev) => ShowConsole());
                menu.Items.Add("-");
                menu.Items.Add("Sair e Parar Serviço", null, (s, ev) => ExitApplication());
                DebugLog("Menu de contexto criado.");

                trayIcon = new NotifyIcon();
                trayIcon.Icon = SystemIcons.Shield;
                trayIcon.Text = "I/O Lockdown";
                trayIcon.ContextMenuStrip = menu;
                trayIcon.Visible = true;
                trayIcon.MouseClick += (s, ev) => { if (ev.Button == MouseButtons.Left) ShowConsole(); };
                DebugLog("NotifyIcon configurado.");

                // Testamos iniciar o monitor Bluetooth com try-catch isolado
                try {
                    DebugLog("Tentando iniciar Bluetooth Monitor...");
                    _engine.StartBluetoothMonitor("Meu Celular");
                    DebugLog("Bluetooth Monitor iniciado.");
                } catch (Exception exBT) {
                    DebugLog($"AVISO: Erro ao iniciar Bluetooth: {exBT.Message}");
                }

                _engine.Log("Interface de usuário carregada com sucesso.");
                DebugLog("Load finalizado.");
            }
            catch (Exception ex)
            {
                DebugLog($"ERRO FATAL NO LOAD: {ex.Message}\n{ex.StackTrace}");
            }
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
            try {
                _engine.SetNetworkState(true);
                _engine.SetUsbStorageState(true);
                _engine.SetUsbHardwareState(true);
            } catch { }

            if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); }
            Environment.Exit(0);
        }

        private void RefreshLogs()
        {
            if (!this.Visible) return;
            try {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lockdown.log");
                if (File.Exists(logPath)) {
                    using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs)) {
                        rtbLogs.Text = sr.ReadToEnd();
                        rtbLogs.SelectionStart = rtbLogs.Text.Length;
                        rtbLogs.ScrollToCaret();
                    }
                }
            } catch { }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (_isLocked && m.Msg == WM_DEVICECHANGE)
            {
                int eventType = m.WParam.ToInt32();
                if (eventType == DBT_DEVICEARRIVAL || eventType == DBT_DEVICEREMOVECOMPLETE)
                {
                    _ = _engine.TriggerViolation("Mudança de hardware detectada.");
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); }
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason) {
                case SessionSwitchReason.SessionLock:
                    _isLocked = true;
                    _engine.CaptureHardwareWhitelist();
                    _engine.SetNetworkState(false);
                    _engine.SetUsbStorageState(false);
                    break;
                case SessionSwitchReason.SessionUnlock:
                    _isLocked = false;
                    if (!_engine.ViolationDetected) {
                        _engine.SetUsbStorageState(true);
                        _engine.SetNetworkState(true);
                    }
                    break;
            }
        }
    }
}
