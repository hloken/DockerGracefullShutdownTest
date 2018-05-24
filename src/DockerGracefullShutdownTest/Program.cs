using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DockerGracefullShutdownTest
{
    class Program
    {
        [DllImport("Kernel32")]
        internal static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        internal delegate bool HandlerRoutine(CtrlTypes CtrlType);

        const int SIGINT = 2; // Ctrl-C
        const int SIGFPE = 8;
        const int SIGTERM = 15; // process termination
        const int WM_CLOSE = 0x0010;

        internal enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        static void Main(string[] args)
        {
            // do the server starting code
            //Start();
                //....

            var shutdown = new ManualResetEvent(false);
            var complete = new ManualResetEventSlim();
            var hr = new HandlerRoutine(type =>
            {
                Console.WriteLine($"ConsoleCtrlHandler got signal: {type}");

                shutdown.Set();
                complete.Wait();

                return false;
            });
            SetConsoleCtrlHandler(hr, true);

            Console.WriteLine("Waiting on handler to trigger...");

            shutdown.WaitOne();

            Console.WriteLine("Stopping server...");

            // do the server stopping code
            //Stop();

            complete.Set();
            GC.KeepAlive(hr);
        }
    }

}
