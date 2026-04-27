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
                // Extrai o ícone associado ao arquivo .exe para garantir sincronia com o Explorer
                try {
                    trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                } catch {
                    trayIcon.Icon = this.Icon; // Fallback para o ícone do Form
                }
                trayIcon.Text = "I/O Lockdown";
                trayIcon.ContextMenuStrip = menu;
                trayIcon.Visible = true;
                trayIcon.MouseClick += (s, ev) => { if (ev.Button == MouseButtons.Left) ShowConsole(); };
                DebugLog("NotifyIcon configurado.");

                UpdateWhitelistUI();
                RefreshBluetoothList();

                _engine.Log("Interface de usuário carregada com sucesso.");
                DebugLog("Load finalizado.");
            }
            catch (Exception ex)
            {
                DebugLog($"ERRO FATAL NO LOAD: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void RefreshBluetoothList()
        {
            try
            {
                cmbBluetoothDevices.Items.Clear();
                var devices = _engine.GetPairedBluetoothDevices();
                if (devices.Count == 0)
                {
                    _engine.Log("Nenhum dispositivo Bluetooth pareado encontrado.");
                    return;
                }

                foreach (var name in devices)
                {
                    cmbBluetoothDevices.Items.Add(name);
                }

                if (cmbBluetoothDevices.Items.Count > 0)
                    cmbBluetoothDevices.SelectedIndex = 0;
            }
            catch (Exception ex) { _engine.Log("Erro ao atualizar lista Bluetooth: " + ex.Message); }
        }

        private void btnRefreshBluetooth_Click(object sender, EventArgs e)
        {
            RefreshBluetoothList();
        }

        private void UpdateWhitelistUI()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateWhitelistUI));
                return;
            }

            try
            {
                _engine.CaptureHardwareWhitelist();
                lstWhitelist.Items.Clear();
                foreach (var id in _engine.TrustedDeviceIds)
                {
                    lstWhitelist.Items.Add(id);
                }
            }
            catch (Exception ex) { _engine.Log("Erro ao atualizar UI da Whitelist: " + ex.Message); }
        }

        private void btnSaveBluetooth_Click(object sender, EventArgs e)
        {
            if (cmbBluetoothDevices.SelectedItem == null)
            {
                MessageBox.Show("Por favor, selecione um dispositivo Bluetooth na lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedItem = cmbBluetoothDevices.SelectedItem.ToString() ?? "";
            
            if (selectedItem.Contains("(Desconectado)"))
            {
                MessageBox.Show("Não é possível monitorar um dispositivo desconectado. Certifique-se de que ele está ligado e conectado ao Windows.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Extrai o endereço MAC entre os colchetes [ ]
            string targetAddress = "";
            int start = selectedItem.LastIndexOf("[");
            int end = selectedItem.LastIndexOf("]");
            if (start >= 0 && end > start)
            {
                targetAddress = selectedItem.Substring(start + 1, end - start - 1);
            }

            if (string.IsNullOrEmpty(targetAddress))
            {
                MessageBox.Show("Não foi possível identificar o endereço do dispositivo.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _engine.StartBluetoothMonitor(targetAddress);
            
            btnSaveBluetooth.Enabled = false;
            btnRefreshBluetooth.Enabled = false;
            cmbBluetoothDevices.Enabled = false;
            
            string displayLabel = selectedItem.Substring(0, start).Trim();
            lblBtInfo.Text = $"Monitorando: {displayLabel}. Para alterar, reinicie o aplicativo.";
            _engine.Log($"Monitoramento Bluetooth configurado para endereço: {targetAddress}");
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
                    _engine.IsLocked = true;
                    UpdateWhitelistUI();
                    _engine.SetNetworkState(false);
                    _engine.SetUsbStorageState(false);
                    break;
                case SessionSwitchReason.SessionUnlock:
                    _isLocked = false;
                    _engine.IsLocked = false;
                    if (!_engine.ViolationDetected) {
                        _engine.SetUsbStorageState(true);
                        _engine.SetNetworkState(true);
                    }
                    break;
            }
        }
    }
}
