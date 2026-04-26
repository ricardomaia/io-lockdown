using System.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace io_lockdown
{
    public class LockdownEngine
    {
        private List<string> _trustedDeviceIds = new List<string>();
        private bool _violationDetected = false;
        private string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lockdown.log");

        public bool ViolationDetected => _violationDetected;

        public void Log(string message)
        {
            try
            {
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(_logPath, entry);
            }
            catch { }
        }

        public void TriggerViolation(string reason)
        {
            if (_violationDetected) return;
            _violationDetected = true;
            Log($"VIOLAÇÃO DETECTADA: {reason}. Iniciando Lockdown Total.");
            SetNetworkState(false);
            SetUsbHardwareState(false);
        }

        public void CaptureKeyboardWhitelist()
        {
            _trustedDeviceIds.Clear();
            try
            {
                SelectQuery query = new SelectQuery("SELECT * FROM Win32_Keyboard");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject device in searcher.Get())
                    {
                        string? id = device["DeviceID"]?.ToString();
                        if (id != null)
                        {
                            _trustedDeviceIds.Add(id);
                        }
                    }
                }
                Log($"Whitelist: {_trustedDeviceIds.Count} teclados.");
            }
            catch (Exception ex) { Log("Erro Whitelist: " + ex.Message); }
        }

        public void SetNetworkState(bool enable)
        {
            if (_violationDetected && enable) return;
            try
            {
                string methodName = enable ? "Enable" : "Disable";
                SelectQuery wmiQuery = new SelectQuery("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionId != NULL");
                using (ManagementObjectSearcher searchProcedure = new ManagementObjectSearcher(wmiQuery))
                {
                    foreach (ManagementObject item in searchProcedure.Get())
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
                    if (key != null)
                    {
                        key.SetValue("Start", enable ? 3 : 4, RegistryValueKind.DWord);
                    }
                }
                Log($"USB Storage: {(enable ? "Ativo" : "Bloqueado")}");
            }
            catch (Exception ex) { Log("Erro USB Storage: " + ex.Message); }
        }
    }
}
