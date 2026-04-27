using System.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Media.MediaProperties;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;

namespace io_lockdown
{
    public class LockdownEngine
    {
        private List<string> _trustedDeviceIds = new List<string>();
        private bool _violationDetected = false;
        private bool _isLocked = false;
        private string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lockdown.log");
        private string _violationDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "violations");
        private ManagementEventWatcher? _usbWatcher;

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        public bool ViolationDetected => _violationDetected;
        public List<string> TrustedDeviceIds => _trustedDeviceIds;
        public bool IsLocked { get => _isLocked; set => _isLocked = value; }

        public LockdownEngine()
        {
            try {
                if (!Directory.Exists(_violationDir)) Directory.CreateDirectory(_violationDir);
                CaptureHardwareWhitelist();
                StartUsbRemovalMonitor();
            } catch { }
        }

        public void PlayAlarm()
        {
            Task.Run(() => {
                try {
                    // Try to play Multimedia System Sound (Critical Stop)
                    System.Media.SystemSounds.Hand.Play();
                    
                    // Also trigger the Beep (often falls back to internal speaker if audio drivers are missing)
                    for (int i = 0; i < 5; i++) {
                        Console.Beep(1500, 400);
                        Console.Beep(1000, 400);
                    }
                } catch { 
                    // Fallback to basic Beep if SystemSounds fails
                    try { Console.Beep(); } catch { }
                }
            });
        }

        public void StartUsbRemovalMonitor()
        {
            try {
                if (_usbWatcher != null) return;

                var query = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
                _usbWatcher = new ManagementEventWatcher(query);
                _usbWatcher.EventArrived += (s, e) => {
                    if (_isLocked) return;

                    var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                    string id = instance["PNPDeviceID"]?.ToString() ?? "";
                    
                    if (!string.IsNullOrEmpty(id) && IsDeviceAuthorized(id)) {
                        Log($"TRUSTED DEVICE REMOVED: {id}. Locking system.");
                        LockWorkStation();
                        PlayAlarm();
                    }
                };
                _usbWatcher.Start();
                Log("USB removal monitor activated.");
            } catch (Exception ex) { Log("Error starting USB monitor: " + ex.Message); }
        }

        public bool IsDeviceAuthorized(string pnpDeviceId)
        {
            if (string.IsNullOrEmpty(pnpDeviceId)) return true;
            return _trustedDeviceIds.Contains(pnpDeviceId);
        }

        public void Log(string message)
        {
            try
            {
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(_logPath, entry);
            }
            catch { }
        }

        public async Task TriggerViolation(string reason)
        {
            if (_violationDetected) return;
            _violationDetected = true;
            Log($"VIOLATION DETECTED: {reason}. Initiating Lockdown.");
            
            await CapturePhoto();
            
            PlayAlarm();
            SetNetworkState(false);
            SetUsbHardwareState(false);
        }

        private async Task CapturePhoto()
        {
            try
            {
                Log("Attempting to capture photo of intruder...");
                var capture = new MediaCapture();
                await capture.InitializeAsync(new MediaCaptureInitializationSettings {
                    StreamingCaptureMode = StreamingCaptureMode.Video
                });

                string fileName = $"violation_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var storageFile = await KnownFolders.PicturesLibrary.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                await capture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), storageFile);
                Log($"Photo saved to: {storageFile.Path}");
            }
            catch (Exception ex) { Log("Error capturing photo: " + ex.Message); }
        }

        public void CaptureHardwareWhitelist()
        {
            _trustedDeviceIds.Clear();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT PNPDeviceID FROM Win32_PnPEntity WHERE Present = True"))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementBaseObject device in collection)
                    {
                        try 
                        {
                            var id = device.GetPropertyValue("PNPDeviceID")?.ToString();
                            if (!string.IsNullOrEmpty(id)) _trustedDeviceIds.Add(id);
                        }
                        catch { }
                        finally { device.Dispose(); }
                    }
                }
                Log($"Global Whitelist: {_trustedDeviceIds.Count} devices monitored.");
            }
            catch (Exception ex) { Log("Global Whitelist error: " + ex.Message); }
        }

        public List<string> GetPairedBluetoothDevices()
        {
            var deviceNames = new List<string>();
            try
            {
                var client = new BluetoothClient();
                var devices = client.PairedDevices; 
                foreach (var d in devices)
                {
                    if (!string.IsNullOrEmpty(d.DeviceName))
                    {
                        string status = d.Connected ? "Connected" : "Disconnected";
                        deviceNames.Add($"{d.DeviceName} ({status}) [{d.DeviceAddress}]");
                    }
                }
            }
            catch (Exception ex) { Log("Error listing Bluetooth devices: " + ex.Message); }
            return deviceNames;
        }

        public void StartBluetoothMonitor(string targetAddress)
        {
            if (string.IsNullOrEmpty(targetAddress)) return;

            Task.Run(async () => {
                var client = new BluetoothClient();
                Log($"Bluetooth monitor started for address: {targetAddress}.");
                int failureCount = 0;
                
                while (true)
                {
                    if (!_isLocked)
                    {
                        bool isActuallyConnected = false;
                        try {
                            var devices = client.PairedDevices;
                            foreach (var d in devices) {
                                if (d.DeviceAddress.ToString() == targetAddress) {
                                    isActuallyConnected = d.Connected;
                                    break;
                                }
                            }
                        } catch { }

                        if (!isActuallyConnected) {
                            failureCount++;
                            Log($"Bluetooth connection missing ({failureCount}/3).");
                            
                            if (failureCount >= 3) {
                                Log("Bluetooth device disconnected. Locking screen...");
                                LockWorkStation();
                                failureCount = 0; 
                            }
                        } else {
                            if (failureCount > 0) Log("Bluetooth device verified as connected.");
                            failureCount = 0;
                        }
                    }
                    else
                    {
                        failureCount = 0; 
                    }
                    
                    await Task.Delay(15000); 
                }
            });
        }

        public void SetNetworkState(bool enable)
        {
            if (_violationDetected && enable) return;
            try
            {
                string methodName = enable ? "Enable" : "Disable";
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionId != NULL"))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject item in collection)
                    {
                        try { item.InvokeMethod(methodName, null); } 
                        catch { }
                        finally { item.Dispose(); }
                    }
                }
                Log($"Network: {methodName}");
            }
            catch (Exception ex) { Log("Network error: " + ex.Message); }
        }

        public void SetUsbHardwareState(bool enable)
        {
            if (_violationDetected && enable) return;
            try
            {
                string action = enable ? "Enable-PnpDevice" : "Disable-PnpDevice";
                string script = $"Get-PnpDevice -Class 'USB' | {action} -Confirm:$false";
                ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -WindowStyle Hidden -Command \"{script}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit();
                Log($"USB Hardware: {action}");
            }
            catch (Exception ex) { Log("USB HW error: " + ex.Message); }
        }

        public void SetUsbStorageState(bool enable)
        {
            if (_violationDetected && enable) return;
            try
            {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\USBSTOR", true))
                {
                    if (key != null) key.SetValue("Start", enable ? 3 : 4, RegistryValueKind.DWord);
                }
                Log($"USB Storage: {(enable ? "Active" : "Blocked")}");
            }
            catch (Exception ex) { Log("USB Storage error: " + ex.Message); }
        }
    }
}
