using System.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Media.MediaProperties;

namespace io_lockdown
{
    public class WindowsHardwareController : IHardwareController
    {
        [DllImport("user32.dll")]
        private static extern bool LockWorkStationInternal();

        public bool LockWorkStation() => LockWorkStationInternal();

        public void PlayAlarm()
        {
            Task.Run(() => {
                try {
                    System.Media.SystemSounds.Hand.Play();
                    for (int i = 0; i < 5; i++) {
                        Console.Beep(1500, 400);
                        Console.Beep(1000, 400);
                    }
                } catch { 
                    try { Console.Beep(); } catch { }
                }
            });
        }

        public async Task CapturePhoto()
        {
            try {
                var capture = new MediaCapture();
                await capture.InitializeAsync(new MediaCaptureInitializationSettings {
                    StreamingCaptureMode = StreamingCaptureMode.Video
                });
                string fileName = $"violation_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var storageFile = await KnownFolders.PicturesLibrary.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                await capture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), storageFile);
            } catch { }
        }

        public List<string> GetCurrentPnpDevices()
        {
            var ids = new List<string>();
            try {
                using (var searcher = new ManagementObjectSearcher("SELECT PNPDeviceID FROM Win32_PnPEntity WHERE Present = True"))
                using (var collection = searcher.Get()) {
                    foreach (ManagementBaseObject device in collection) {
                        var id = device.GetPropertyValue("PNPDeviceID")?.ToString();
                        if (!string.IsNullOrEmpty(id)) ids.Add(id);
                    }
                }
            } catch { }
            return ids;
        }

        public void SetNetworkState(bool enable)
        {
            try {
                string methodName = enable ? "Enable" : "Disable";
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter = True AND NetConnectionId != NULL"))
                using (var collection = searcher.Get()) {
                    foreach (ManagementObject item in collection) {
                        try { item.InvokeMethod(methodName, null); } catch { }
                    }
                }
            } catch { }
        }

        public void SetUsbHardwareState(bool enable)
        {
            try {
                string action = enable ? "Enable-PnpDevice" : "Disable-PnpDevice";
                string script = $"Get-PnpDevice -Class 'USB' | {action} -Confirm:$false";
                ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -WindowStyle Hidden -Command \"{script}\"") {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit();
            } catch { }
        }

        public void SetUsbStorageState(bool enable)
        {
            try {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\USBSTOR", true)) {
                    if (key != null) key.SetValue("Start", enable ? 3 : 4, RegistryValueKind.DWord);
                }
            } catch { }
        }
    }
}
