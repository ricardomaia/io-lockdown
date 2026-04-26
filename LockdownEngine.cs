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
        private string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lockdown.log");
        private string _violationDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "violations");

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        public bool ViolationDetected => _violationDetected;

        public LockdownEngine()
        {
            try {
                if (!Directory.Exists(_violationDir)) Directory.CreateDirectory(_violationDir);
            } catch { }
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
            Log($"VIOLAÇÃO DETECTADA: {reason}. Iniciando Lockdown.");
            
            await CapturePhoto();
            
            SetNetworkState(false);
            SetUsbHardwareState(false);
        }

        private async Task CapturePhoto()
        {
            try
            {
                Log("Tentando capturar foto do intruso...");
                var capture = new MediaCapture();
                await capture.InitializeAsync(new MediaCaptureInitializationSettings {
                    StreamingCaptureMode = StreamingCaptureMode.Video
                });

                string fileName = $"violation_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var storageFile = await KnownFolders.PicturesLibrary.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                await capture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), storageFile);
                Log($"Foto salva em: {storageFile.Path}");
            }
            catch (Exception ex) { Log("Erro ao capturar foto: " + ex.Message); }
        }

        public void CaptureHardwareWhitelist()
        {
            _trustedDeviceIds.Clear();
            try
            {
                string[] queries = { "SELECT PNPDeviceID FROM Win32_Keyboard", "SELECT PNPDeviceID FROM Win32_PointingDevice" };
                foreach (var q in queries)
                {
                    using (var searcher = new ManagementObjectSearcher(new SelectQuery(q)))
                    {
                        foreach (ManagementObject device in searcher.Get())
                        {
                            string id = device["PNPDeviceID"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(id)) _trustedDeviceIds.Add(id);
                        }
                    }
                }
                Log($"Whitelist: {_trustedDeviceIds.Count} dispositivos.");
            }
            catch (Exception ex) { Log("Erro Whitelist: " + ex.Message); }
        }

        public void StartBluetoothMonitor(string targetDeviceName)
        {
            if (string.IsNullOrEmpty(targetDeviceName) || targetDeviceName == "Meu Celular") return;

            Task.Run(async () => {
                var client = new BluetoothClient();
                Log($"Monitor Bluetooth iniciado para: {targetDeviceName}");
                
                while (true)
                {
                    bool found = false;
                    try {
                        // Na v4, usamos DiscoverDevices()
                        var devices = client.DiscoverDevices();
                        foreach (var d in devices) {
                            if (d.DeviceName.Contains(targetDeviceName)) {
                                found = true;
                                break;
                            }
                        }
                    } catch { }

                    if (!found) {
                        Log("Dispositivo Bluetooth ausente! Bloqueando...");
                        LockWorkStation();
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
                using (var searcher = new ManagementObjectSearcher(new SelectQuery("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionId != NULL")))
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        item.InvokeMethod(methodName, null);
                    }
                }
                Log($"Rede: {methodName}");
            }
            catch (Exception ex) { Log("Erro Rede: " + ex.Message); }
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
            catch (Exception ex) { Log("Erro USB HW: " + ex.Message); }
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
                Log($"USB Storage: {(enable ? "Ativo" : "Bloqueado")}");
            }
            catch (Exception ex) { Log("Erro USB Storage: " + ex.Message); }
        }
    }
}
