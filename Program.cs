using System;
using System.Net;
using System.Windows.Forms;
using GachaLinkFetcher.UI;

internal static class Program
{
    [STAThread] private static void Main()
    {
        // The legacy .NET Framework compiler can otherwise negotiate TLS 1.0,
        // while current official game record endpoints require TLS 1.2.
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm());
    }
}
