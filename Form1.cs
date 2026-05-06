using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Management;
using System.Threading.Tasks;

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
            
            try {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            } catch { }

            lblVersion.Text = $"v{Application.ProductVersion.Substring(0, 5)}";

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
            DebugLog("Starting Form1_Load");
            try
            {
                SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
                DebugLog("Session events registered.");

                var menu = new ContextMenuStrip();
                var versionItem = new ToolStripMenuItem($"I/O Lockdown {lblVersion.Text}");
                versionItem.Enabled = false;
                versionItem.Font = new Font(this.Font, FontStyle.Bold);
                menu.Items.Add(versionItem);
                menu.Items.Add("-");
                menu.Items.Add("Show Audit Console", null, (s, ev) => ShowConsole());
                menu.Items.Add("-");
                menu.Items.Add("Exit and Stop Service", null, (s, ev) => ExitApplication());
                DebugLog("Context menu created.");

                trayIcon = new NotifyIcon();
                trayIcon.Icon = this.Icon; 
                trayIcon.Text = "I/O Lockdown";
                trayIcon.ContextMenuStrip = menu;
                trayIcon.Visible = true;
                trayIcon.MouseClick += (s, ev) => { if (ev.Button == MouseButtons.Left) ShowConsole(); };
                DebugLog("NotifyIcon configured.");

                RefreshWhitelistListOnly();
                RefreshBluetoothList();

                _engine.Log("User interface loaded successfully.");
                DebugLog("Load finished.");

                // Start minimized to tray
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.BeginInvoke(new MethodInvoker(this.Hide));
            }
            catch (Exception ex)
            {
                DebugLog($"FATAL ERROR ON LOAD: {ex.Message}\n{ex.StackTrace}");
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
                    _engine.Log("No paired Bluetooth devices found.");
                    return;
                }

                foreach (var name in devices)
                {
                    cmbBluetoothDevices.Items.Add(name);
                }

                if (cmbBluetoothDevices.Items.Count > 0)
                    cmbBluetoothDevices.SelectedIndex = 0;
            }
            catch (Exception ex) { _engine.Log("Error updating Bluetooth list: " + ex.Message); }
        }

        private void btnRefreshBluetooth_Click(object sender, EventArgs e)
        {
            RefreshBluetoothList();
        }

        private void RefreshWhitelistListOnly()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(RefreshWhitelistListOnly));
                return;
            }

            try
            {
                lstWhitelist.Items.Clear();
                foreach (var id in _engine.TrustedDeviceIds)
                {
                    lstWhitelist.Items.Add(id);
                }
            }
            catch (Exception ex) { _engine.Log("Error updating Whitelist UI: " + ex.Message); }
        }

        private void btnSaveBluetooth_Click(object sender, EventArgs e)
        {
            if (cmbBluetoothDevices.SelectedItem == null)
            {
                MessageBox.Show("Please select a Bluetooth device from the list.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedItem = cmbBluetoothDevices.SelectedItem?.ToString() ?? "";
            
            if (string.IsNullOrEmpty(selectedItem) || selectedItem.Contains("(Disconnected)"))
            {
                MessageBox.Show("Please select a connected Bluetooth device.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string targetAddress = "";
            int start = selectedItem.LastIndexOf("[");
            int end = selectedItem.LastIndexOf("]");
            if (start >= 0 && end > start)
            {
                targetAddress = selectedItem.Substring(start + 1, end - start - 1);
            }

            if (string.IsNullOrEmpty(targetAddress))
            {
                MessageBox.Show("Could not identify the device address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _engine.StartBluetoothMonitor(targetAddress);
            
            btnSaveBluetooth.Enabled = false;
            btnRefreshBluetooth.Enabled = false;
            cmbBluetoothDevices.Enabled = false;
            
            string displayLabel = selectedItem.Substring(0, start).Trim();
            lblBtInfo.Text = $"Monitoring: {displayLabel}. To change, restart the application.";
            _engine.Log($"Bluetooth monitoring configured for address: {targetAddress}");
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear the violation state and restore all hardware?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _engine.ResetSystemToSafeState();
                lblStatus.Text = "Status: Protection Active";
                lblStatus.ForeColor = Color.Black;
                RefreshLogs();
            }
        }

        private void ShowConsole()
        {
            this.Show();
            this.ShowInTaskbar = true;
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

            if (_engine.ViolationDetected)
            {
                lblStatus.Text = "Status: VIOLATION DETECTED - SYSTEM LOCKED";
                lblStatus.ForeColor = Color.Red;
            }

            try {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lockdown.log");
                if (File.Exists(logPath)) {
                    using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs)) {
                        string newContent = sr.ReadToEnd();
                        if (rtbLogs.Text != newContent)
                        {
                            rtbLogs.Text = newContent;
                            rtbLogs.SelectionStart = rtbLogs.Text.Length;
                            rtbLogs.ScrollToCaret();
                        }
                    }
                }
            } catch { }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_DEVICECHANGE)
            {
                int eventType = m.WParam.ToInt32();

                if (eventType == DBT_DEVICEARRIVAL)
                {
                    Task.Run(() => CheckNewHardwareIntegrity());
                }
                else if (eventType == DBT_DEVICEREMOVECOMPLETE && !_isLocked)
                {
                    Task.Run(() => CheckTrustedDeviceRemoval());
                }

                if (_isLocked && (eventType == DBT_DEVICEARRIVAL || eventType == DBT_DEVICEREMOVECOMPLETE))
                {
                    _ = _engine.TriggerViolation("Hardware change detected during lockdown.");
                }
            }
        }

        private void CheckTrustedDeviceRemoval()
        {
            try
            {
                // Brief delay so the OS updates the PnP device list before we query it
                System.Threading.Thread.Sleep(500);
                var currentSet = new HashSet<string>(_engine.GetCurrentPnpDevices(), StringComparer.OrdinalIgnoreCase);

                foreach (var id in _engine.TrustedDeviceIds.ToList())
                {
                    if (!currentSet.Contains(id))
                    {
                        _engine.Log($"TRUSTED DEVICE REMOVED: {id}. Locking workstation.");
                        _engine.SetNetworkState(false);
                        _engine.SetUsbStorageState(false);
                        _engine.RequestLock($"TRUSTED DEVICE REMOVED: {id}");
                        return;
                    }
                }
            }
            catch (Exception ex) { _engine.Log("Error checking device removal: " + ex.Message); }
        }

        private void CheckNewHardwareIntegrity()
        {
            try
            {
                foreach (var id in _engine.GetCurrentPnpDevices())
                {
                    if (!string.IsNullOrEmpty(id) && !_engine.IsDeviceAuthorized(id))
                    {
                        _engine.Log($"UNAUTHORIZED DEVICE DETECTED: {id}");
                        _ = _engine.TriggerViolation($"New unauthorized hardware: {id}");
                        LockdownEngine.LockWorkStation();
                        return;
                    }
                }
            }
            catch (Exception ex) { _engine.Log("Error in PnP integrity check: " + ex.Message); }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); this.ShowInTaskbar = false; }
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason) {
                case SessionSwitchReason.SessionLock:
                    _isLocked = true;
                    _engine.IsLocked = true;
                    _engine.CaptureHardwareWhitelist();
                    RefreshWhitelistListOnly();
                    _engine.SetNetworkState(false);
                    _engine.SetUsbStorageState(false);
                    break;
                case SessionSwitchReason.SessionUnlock:
                    _isLocked = false;
                    _engine.IsLocked = false;
                    if (!_engine.ViolationDetected) {
                        _engine.ResetSystemToSafeState();
                    }
                    break;
            }
        }
    }
}
