using System;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace io_lockdown
{
    internal static class Program
    {
        private static Mutex mutex = new Mutex(true, "{73a1e9c2-5b12-4e92-910f-226cd97ff4f1}-UI");
        // MUDANÇA: Log na pasta TEMP para evitar erros de permissão durante o debug
        private static string logPath = Path.Combine(Path.GetTempPath(), "iolockdown_debug.log");

        private static void Log(string message)
        {
            try { File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] [DEBUG-MAIN] {message}{Environment.NewLine}"); } catch { }
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.ThreadException += (s, e) => Log($"ERRO DE THREAD: {e.Exception}");
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log($"ERRO CRÍTICO: {e.ExceptionObject}");

            Log("--- INÍCIO DE EXECUÇÃO ---");

            try
            {
                if (args.Contains("--service"))
                {
                    Log("Iniciando como Serviço.");
                    ServiceBase[] ServicesToRun = new ServiceBase[] { new LockdownService() };
                    ServiceBase.Run(ServicesToRun);
                    return;
                }

                Log("Iniciando Verificação de Instância Única.");
                if (!mutex.WaitOne(TimeSpan.Zero, true))
                {
                    Log("Outra instância detectada. Abortando.");
                    return;
                }

                Log("Configurando WinForms.");
                ApplicationConfiguration.Initialize();
                
                Log("Criando Form1.");
                Form1 f = new Form1();
                
                Log("Iniciando Application.Run.");
                Application.Run(f);
            }
            catch (Exception ex)
            {
                Log($"FALHA FATAL: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
