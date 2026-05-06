using System.Collections.Generic;
using System.Threading.Tasks;

namespace io_lockdown
{
    public interface IHardwareController
    {
        void SetNetworkState(bool enable);
        void SetUsbHardwareState(bool enable);
        void SetUsbStorageState(bool enable);
        List<string> GetCurrentPnpDevices();
        void PlayAlarm();
        Task CapturePhoto();
        bool LockWorkStation();
    }
}
