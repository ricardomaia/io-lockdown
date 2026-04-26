using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace io_lockdown
{
    public partial class Form1 : Form
    {
        private NotifyIcon? trayIcon;
        private LockdownEngine _engine = new LockdownEngine();
        private bool _isLocked = false;

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            
            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Shield;
            trayIcon.Text = "I/O Lockdown Ativo";
            trayIcon.Visible = true;
            
            _engine.Log("Interface de usuário iniciada.");
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (_isLocked && m.Msg == WM_DEVICECHANGE)
            {
                int eventType = m.WParam.ToInt32();
                if (eventType == DBT_DEVICEARRIVAL || eventType == DBT_DEVICEREMOVECOMPLETE)
                {
                    _engine.TriggerViolation("Mudança de hardware detectada via Interface.");
                }
            }
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            _engine.Log($"Evento de sessão (UI): {e.Reason}");
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    _isLocked = true;
                    _engine.CaptureKeyboardWhitelist();
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
                    else
                    {
                        MessageBox.Show("ALERTA: Violação detectada. Hardware bloqueado.", "Segurança", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    }
                    break;
            }
        }
    }
}
