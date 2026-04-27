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

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        public bool ViolationDetected => _violationDetected;
        public List<string> TrustedDeviceIds => _trustedDeviceIds;
        public bool IsLocked { get => _isLocked; set => _isLocked = value; }

        public LockdownEngine()
        {
            try {
                if (!Directory.Exists(_violationDir)) Directory.CreateDirectory(_violationDir);
                CaptureHardwareWhitelist(); // Captura base inicial de confiança
            } catch { }
        }

        public bool IsDeviceAuthorized(string pnpDeviceId)
        {
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
                // Captura TODOS os dispositivos PnP atualmente presentes no sistema
                using (var searcher = new ManagementObjectSearcher(new SelectQuery("SELECT PNPDeviceID FROM Win32_PnPEntity WHERE Present = True")))
                {
                    foreach (ManagementObject device in searcher.Get())
                    {
                        string id = device["PNPDeviceID"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(id)) _trustedDeviceIds.Add(id);
                    }
                }
                Log($"Whitelist Global: {_trustedDeviceIds.Count} dispositivos monitorados.");
            }
            catch (Exception ex) { Log("Erro Whitelist Global: " + ex.Message); }
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
                        string status = d.Connected ? "Conectado" : "Desconectado";
                        // Incluímos o endereço no final para identificação única
                        deviceNames.Add($"{d.DeviceName} ({status}) [{d.DeviceAddress}]");
                    }
                }
            }
            catch (Exception ex) { Log("Erro ao listar Bluetooth: " + ex.Message); }
            return deviceNames;
        }

        public void StartBluetoothMonitor(string targetAddress)
        {
            if (string.IsNullOrEmpty(targetAddress)) return;

            Task.Run(async () => {
                var client = new BluetoothClient();
                Log($"Monitor Bluetooth iniciado para endereço: {targetAddress}.");
                int failureCount = 0;
                
                while (true)
                {
                    if (!_isLocked)
                    {
                        bool isActuallyConnected = false;
                        try {
                            // Verificamos todos os dispositivos pareados e checamos o status de conexão do endereço específico
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
                            Log($"Conexão Bluetooth ausente ({failureCount}/3).");
                            
                            if (failureCount >= 3) {
                                Log("Dispositivo Bluetooth desconectado. Bloqueando tela...");
                                LockWorkStation();
                                failureCount = 0; 
                            }
                        } else {
                            if (failureCount > 0) Log("Dispositivo Bluetooth verificado como conectado.");
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
