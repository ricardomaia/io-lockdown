using System;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;
using System.Diagnostics;

namespace io_lockdown
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Contains("--service"))
            {
                ServiceBase[] ServicesToRun = new ServiceBase[] { new LockdownService() };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                // Verifica se já existe OUTRA instância da UI (ignorando o serviço)
                Process current = Process.GetCurrentProcess();
                bool isAnotherUI = Process.GetProcessesByName(current.ProcessName)
                    .Any(p => p.Id != current.Id && p.SessionId != 0);

                if (isAnotherUI)
                {
                    return; // Já existe uma interface rodando nesta sessão de usuário
                }

                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());
            }
        }
    }
}
