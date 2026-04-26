using System;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;

namespace io_lockdown
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Contains("--service"))
            {
                // Roda como serviço do Windows
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new LockdownService()
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                // Roda como aplicação Windows Forms normal
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());
            }
        }
    }
}
