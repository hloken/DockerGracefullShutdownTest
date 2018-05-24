using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace DockerGracefullShutdownTest
{
    class Program
    {
        [DllImport("Kernel32")]
        internal static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        [DllImport("msvcrt.dll", PreserveSig = true)]
        static extern SignalHandler signal(int sig, SignalHandler handler);

        internal delegate bool HandlerRoutine(CtrlTypes CtrlType);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void SignalHandler(int sig);

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
            var writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "log.txt"), false) { AutoFlush = true };

            Console.CancelKeyPress += (s, e) => writer.WriteLine($"Console.CancelKeyPress {e.SpecialKey}, thread {Thread.CurrentThread.ManagedThreadId}");
            AppDomain.CurrentDomain.ProcessExit += (s, e) => writer.WriteLine($"CurrentDomain.ProcessExit, thread {Thread.CurrentThread.ManagedThreadId}");
            AppDomain.CurrentDomain.DomainUnload += (s, e) => writer.WriteLine($"CurrentDomain.DomainUnload, thread {Thread.CurrentThread.ManagedThreadId}");
            Microsoft.Win32.SystemEvents.SessionEnding += (s, e) => writer.WriteLine($"SystemEvents.SessionEnding {e.Reason}, thread {Thread.CurrentThread.ManagedThreadId}");
            Microsoft.Win32.SystemEvents.SessionEnded += (s, e) => writer.WriteLine($"SystemEvents.SessionEnded {e.Reason}, thread {Thread.CurrentThread.ManagedThreadId}");
            Microsoft.Win32.SystemEvents.SessionSwitch += (s, e) => writer.WriteLine($"SystemEvents.SessionSwitch {e.Reason}, thread {Thread.CurrentThread.ManagedThreadId}");
            Microsoft.Win32.SystemEvents.EventsThreadShutdown += (s, e) => writer.WriteLine($"SystemEvents.EventsThreadShutdown, thread {Thread.CurrentThread.ManagedThreadId}");
            Microsoft.Win32.SystemEvents.PowerModeChanged += (s, e) => writer.WriteLine($"SystemEvents.PowerModeChanged {e.Mode}, thread {Thread.CurrentThread.ManagedThreadId}");

            var hr = new HandlerRoutine(type => { writer.WriteLine($"ConsoleCtrlHandler {type}, thread {Thread.CurrentThread.ManagedThreadId}"); return false; });
            var sh = new SignalHandler(sig => writer.WriteLine($"Got signal {sig} on thread {Thread.CurrentThread.ManagedThreadId}"));

            writer.WriteLine($"SetConsoleCtrlHandler returned {SetConsoleCtrlHandler(hr, true)}");
            writer.WriteLine($"signal returned null: {signal(SIGTERM, sh) == null}");

            new Thread(() =>
            {
                using (var sf = new someform(writer))
                    System.Windows.Forms.Application.Run(sf);
            }).Start();

            writer.WriteLine($"just sitting around waiting, on thread {Thread.CurrentThread.ManagedThreadId}");
            while (true)
                Thread.Sleep(1000);
            writer.WriteLine("uh, we exited the loop, wat");

            GC.KeepAlive(hr);
            GC.KeepAlive(sh);
        }

        class someform : System.Windows.Forms.Form
        {
            private TextWriter m_writer;

            public someform(TextWriter writer)
            {
                SuspendLayout();
                ClientSize = new System.Drawing.Size(0, 0);
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                WindowState = System.Windows.Forms.FormWindowState.Minimized;
                ResumeLayout();

                m_writer = writer;
            }

            protected override void WndProc(ref System.Windows.Forms.Message m)
            {
                if (m.Msg == WM_CLOSE)
                    m_writer.WriteLine($"got WM_CLOSE on thread {Thread.CurrentThread.ManagedThreadId}");
                //else
                //	Console.WriteLine($"got some random message {m.Msg} on thread {Thread.CurrentThread.ManagedThreadId}");

                base.WndProc(ref m);
            }
        }
    }

}
