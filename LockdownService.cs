using System.ServiceProcess;
using Microsoft.Win32;

namespace io_lockdown
{
    public class LockdownService : ServiceBase
    {
        private LockdownEngine _engine = new LockdownEngine();

        public LockdownService()
        {
            ServiceName = "IOLockdownService";
            CanHandleSessionChangeEvent = true;
        }

        protected override void OnStart(string[] args)
        {
            _engine.Log("Serviço I/O Lockdown iniciado no Windows.");
        }

        protected override void OnStop()
        {
            _engine.Log("Serviço I/O Lockdown parado.");
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            _engine.Log($"Evento de sessão (Serviço): {changeDescription.Reason}");

            switch (changeDescription.Reason)
            {
                case SessionChangeReason.SessionLock:
                    _engine.IsLocked = true;
                    _engine.CaptureHardwareWhitelist();
                    _engine.SetNetworkState(false);
                    _engine.SetUsbStorageState(false);
                    break;

                case SessionChangeReason.SessionUnlock:
                    _engine.IsLocked = false;
                    if (!_engine.ViolationDetected)
                    {
                        _engine.SetUsbStorageState(true);
                        _engine.SetNetworkState(true);
                    }
                    break;
            }
        }
    }
}
