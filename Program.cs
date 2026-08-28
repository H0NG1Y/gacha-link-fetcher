using System;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using GachaLinkFetcher.UI;

internal static class Program
{
    private const string MutexName = "Local\\GachaLinkFetcher.SingleInstance";
    private const string LaunchEventName = "Local\\GachaLinkFetcher.SecondaryLaunch";
    private static EventWaitHandle launchEvent;
    private static volatile bool shuttingDown;

    [STAThread]
    private static void Main()
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        bool createdNew;
        using (var instanceMutex = new Mutex(true, MutexName, out createdNew))
        {
            if (!createdNew)
            {
                SignalExistingInstance();
                return;
            }

            using (launchEvent = new EventWaitHandle(false, EventResetMode.AutoReset, LaunchEventName))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var form = new MainForm();
                form.Shown += delegate { StartLaunchListener(form); };
                form.FormClosed += delegate
                {
                    shuttingDown = true;
                    if (launchEvent != null) launchEvent.Set();
                };
                Application.Run(form);
            }
            GC.KeepAlive(instanceMutex);
        }
    }

    private static void SignalExistingInstance()
    {
        AllowSetForegroundWindow(-1);
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                using (var signal = EventWaitHandle.OpenExisting(LaunchEventName))
                {
                    signal.Set();
                    return;
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Thread.Sleep(100);
            }
        }
    }

    private static void StartLaunchListener(MainForm form)
    {
        var listener = new Thread(delegate()
        {
            while (!shuttingDown)
            {
                launchEvent.WaitOne();
                if (shuttingDown || form.IsDisposed) break;
                try { form.BeginInvoke(new Action(form.HandleSecondaryLaunch)); }
                catch (InvalidOperationException) { break; }
            }
        });
        listener.IsBackground = true;
        listener.Name = "GachaLinkFetcher secondary launch listener";
        listener.Start();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int processId);
}
