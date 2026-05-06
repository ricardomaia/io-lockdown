using System.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        private ManagementEventWatcher? _usbRemovalWatcher;
        private ManagementEventWatcher? _usbArrivalWatcher;
        private IHardwareController _hardware;
        private static IHardwareController _staticHardware = new WindowsHardwareController();

        public static void LockWorkStation() => _staticHardware.LockWorkStation();

        public void RequestLock(string reason)
        {
            Log($"LOCK REQUESTED: {reason}");
            bool success = _hardware.LockWorkStation();
            Log($"LockWorkStation result: {success}");
        }

        public bool ViolationDetected => _violationDetected;
        public List<string> TrustedDeviceIds => _trustedDeviceIds;
        public bool IsLocked { get => _isLocked; set => _isLocked = value; }

        public LockdownEngine(IHardwareController? hardware = null)
        {
            _hardware = hardware ?? new WindowsHardwareController();
            try {
                if (!Directory.Exists(_violationDir)) Directory.CreateDirectory(_violationDir);
                ResetSystemToSafeState();
                CaptureHardwareWhitelist();
                StartHardwareMonitors();
            } catch { }
        }

        public void ResetSystemToSafeState()
        {
            Log("Executing Fail-Safe: Restoring hardware and network states.");
            _violationDetected = false;
            _isLocked = false;
            
            _hardware.SetUsbStorageState(true);
            _hardware.SetNetworkState(true);
            _hardware.SetUsbHardwareState(true);
        }

        public void StartHardwareMonitors()
        {
            StartUsbRemovalMonitor();
            StartUsbArrivalMonitor();
        }

        public void StartUsbArrivalMonitor()
        {
            try {
                if (_usbArrivalWatcher != null) return;

                Log("Initializing Arrival monitor...");
                var query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
                _usbArrivalWatcher = new ManagementEventWatcher(query);
                _usbArrivalWatcher.EventArrived += (s, e) => {
                    try {
                        var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                        string id = instance["PNPDeviceID"]?.ToString() ?? "Unknown ID";
                        string name = instance["Name"]?.ToString() ?? "Unknown Name";
                        Log($"EVENT: Hardware Arrival detected: {name} ({id})");
                        
                        if (_isLocked) {
                            _ = TriggerViolation($"Hardware connection detected during lockdown: {id}");
                        } else {
                            if (!IsDeviceAuthorized(id)) {
                                RequestLock($"UNAUTHORIZED DEVICE ARRIVAL: {id}");
                                _ = TriggerViolation($"New unauthorized hardware connected: {id}");
                            }
                        }
                    } catch (Exception ex) { Log("Error processing Arrival event: " + ex.Message); }
                };
                _usbArrivalWatcher.Start();
                Log("Hardware arrival monitor activated.");
            } catch (Exception ex) { Log("Error starting Arrival monitor: " + ex.Message); }
        }

        public void StartUsbRemovalMonitor()
        {
            try {
                if (_usbRemovalWatcher != null) return;

                Log("Initializing Removal monitor...");
                var query = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
                _usbRemovalWatcher = new ManagementEventWatcher(query);
                _usbRemovalWatcher.EventArrived += (s, e) => {
                    try {
                        var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                        string id = instance["PNPDeviceID"]?.ToString() ?? "Unknown ID";
                        string name = instance["Name"]?.ToString() ?? "Unknown Name";
                        Log($"EVENT: Hardware Removal detected: {name} ({id})");
                        
                        if (_isLocked) {
                            _ = TriggerViolation($"Hardware removal detected during lockdown: {id}");
                        } else {
                            if (IsDeviceAuthorized(id)) {
                                RequestLock($"TRUSTED DEVICE REMOVED: {id}");
                                _hardware.PlayAlarm();
                            }
                        }
                    } catch (Exception ex) { Log("Error processing Removal event: " + ex.Message); }
                };
                _usbRemovalWatcher.Start();
                Log("Hardware removal monitor activated.");
            } catch (Exception ex) { Log("Error starting Removal monitor: " + ex.Message); }
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
            
            await _hardware.CapturePhoto();
            _hardware.PlayAlarm();
            _hardware.SetNetworkState(false);
            _hardware.SetUsbHardwareState(false);
        }

        public void CaptureHardwareWhitelist()
        {
            _trustedDeviceIds.Clear();
            try {
                _trustedDeviceIds = _hardware.GetCurrentPnpDevices();
                Log($"Global Whitelist: {_trustedDeviceIds.Count} devices monitored.");
            }
            catch (Exception ex) { Log("Global Whitelist error: " + ex.Message); }
        }

        public List<string> GetPairedBluetoothDevices()
        {
            var deviceNames = new List<string>();
            try {
                var client = new BluetoothClient();
                var devices = client.PairedDevices; 
                foreach (var d in devices) {
                    if (!string.IsNullOrEmpty(d.DeviceName)) {
                        string status = d.Connected ? "Connected" : "Disconnected";
                        deviceNames.Add($"{d.DeviceName} ({status}) [{d.DeviceAddress}]");
                    }
                }
            } catch (Exception ex) { Log("Error listing Bluetooth devices: " + ex.Message); }
            return deviceNames;
        }

        public void StartBluetoothMonitor(string targetAddress)
        {
            if (string.IsNullOrEmpty(targetAddress)) return;

            Task.Run(async () => {
                var client = new BluetoothClient();
                Log($"Bluetooth monitor started for address: {targetAddress}.");
                int failureCount = 0;
                
                while (true) {
                    if (!_isLocked) {
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
                                _hardware.LockWorkStation();
                                failureCount = 0; 
                            }
                        } else {
                            if (failureCount > 0) Log("Bluetooth device verified as connected.");
                            failureCount = 0;
                        }
                    } else {
                        failureCount = 0; 
                    }
                    await Task.Delay(15000); 
                }
            });
        }

        // Delegated methods for backward compatibility if needed, but they use the hardware abstraction
        public void SetNetworkState(bool enable) => _hardware.SetNetworkState(enable);
        public void SetUsbHardwareState(bool enable) => _hardware.SetUsbHardwareState(enable);
        public void SetUsbStorageState(bool enable) => _hardware.SetUsbStorageState(enable);
    }
}
